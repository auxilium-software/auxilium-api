using AuxiliumAPI.Common.CouchDbDocumentConstruction.Structures;
using AuxiliumAPI.Common.DataStructures.MariaDB;
using AuxiliumAPI.Common.Enumerators;
using AuxiliumAPI.Common.Services.Interfaces;
using AuxiliumAPI.Common.Utilities;
using AuxiliumAPI.Models;
using AuxiliumAPI.Models.File;
using AuxiliumAPI.Models.Me;
using AuxiliumAPI.Models.User;
using AuxiliumAPI.Models.UserRegistration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Win32;
using System.Security.Claims;

namespace AuxiliumAPI.Controllers;

[ApiController]
[Route("/api/v3/me")]
[Tags("Account Management")]
[Authorize]
public class MeController : ControllerBase
{
    private readonly ILogger<MeController> _logger;
    private readonly IMariaDbService _mariaDb;
    private readonly ICouchDbService _couchDb;
    private readonly IPasswordService _passwordService;

    public MeController(
        ILogger<MeController> logger,
        IMariaDbService mariaDb,
        ICouchDbService couchDb,
        IPasswordService passwordService
        )
    {
        _logger = logger;
        _mariaDb = mariaDb;
        _couchDb = couchDb;
        _passwordService = passwordService;
    }

    [HttpGet("")]
    [ProducesResponseType(typeof(UserDetailsResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UserDetailsResponseModel>> GetDetailsAboutMyself()
    {
        try
        {
            // get current user id from token
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new FailureResponseModel() { Detail = "User ID not found in token" });
            }
            if (!Guid.TryParse(userId, out _))
            {
                return BadRequest(new FailureResponseModel() { Detail = "You must provide a valid UUID" });
            }

            // get user from mariadb using the user id from the token
            var mariaDbUser = await _mariaDb.QuerySingleOrDefaultAsync<UserRowStructure>(
                "SELECT * FROM users WHERE id = @userId",
                new { userId }
            );

            // if the user is not found, return not found
            if (mariaDbUser == null)
            {
                return NotFound(new FailureResponseModel() { Detail = "User not found" });
            }

            // get user document from couchdb
            var userDoc = await _couchDb.GetDocumentAsync<UserDocumentStructure>(
                ConfigurationUtilities.GetString("Databases", "CouchDB", "Databases", "Users"),
                userId
            );

            // if the user doc is not found, return not found
            if (userDoc == null)
            {
                return NotFound(new FailureResponseModel() { Detail = "User profile not found" });
            }

            // build response with both mariadb and couchdb data
            var response = new UserDetailsResponseModel
            {
                Id = userId,
                CreatedAt = userDoc.CreatedAt,
                CreatedBy = userDoc.CreatedBy,
                LastUpdatedAt = userDoc.CreatedAt,
                LastUpdatedBy = userDoc.LastUpdatedBy,

                AdditionalProperties = userDoc.AdditionalProperties,
                Files = userDoc.Files,

                EmailAddress = mariaDbUser.email_address,
                IsAdmin = mariaDbUser.is_admin,
                FullName = userDoc.FullName,
                FullAddress = userDoc.FullAddress,
                TelephoneNumber = userDoc.TelephoneNumber,
                Gender = userDoc.Gender,
                DateOfBirth = userDoc.DateOfBirth,
                HowDidYouFindOutAboutOurService = userDoc.HowDidYouFindOutAboutOurService,
            };

            // return
            return Ok(
                response
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch user details for current user");
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new FailureResponseModel() { Detail = $"Failed to fetch user details: {ex.Message}" }
            );
        }
    }


    [HttpPost("upload")]
    [ProducesResponseType(typeof(SuccessResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SuccessResponseModel>> UploadFile(
        [FromForm] FileUploadRequestModel request)
    {
        try
        {
            // get current user id from token
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new FailureResponseModel() { Detail = "User ID not found in token" });
            }
            if (!Guid.TryParse(userId, out _))
            {
                return BadRequest(new FailureResponseModel() { Detail = "You must provide a valid UUID" });
            }

            // Validate file
            if (request.File == null || request.File.Length == 0)
            {
                return BadRequest(new FailureResponseModel() { Detail = "No file provided" });
            }

            // read file contents
            using var memoryStream = new MemoryStream();
            await request.File.CopyToAsync(memoryStream);
            var fileBytes = memoryStream.ToArray();

            // get content type
            var contentType = request.File.ContentType ?? "application/octet-stream";

            // create file ID
            var fileId = UUIDUtilities.GenerateV5String(DatabaseObjectType.File);

            // get file hash (SHA256)
            var fileHash = HashingUtilities.SHA256Hash(fileBytes);

            // create document for the file for couchdb
            var fileDoc = new FileDocumentStructure
            {
                Id = fileId,
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Filename = request.File.FileName,
                Description = request.Description,
                ContentType = contentType,
                Hash = fileHash,
                Size = fileBytes.Length,
            };

            // save to couchdb
            await _couchDb.SaveDocumentAsync(
                ConfigurationUtilities.GetString("Databases", "CouchDB", "Databases", "Files"),
                fileDoc
            );

            // return
            return Ok(new SuccessResponseModel());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload file");
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new FailureResponseModel() { Detail = "Failed to upload file" }
            );
        }
    }

    /// <summary>
    /// Change the current user's password
    /// </summary>
    [HttpPost("change-password")]
    [ProducesResponseType(typeof(SuccessResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SuccessResponseModel>> ChangePassword(
        [FromBody] PasswordUpdateRequestModel request)
    {
        await using var transaction = await _mariaDb.BeginTransactionAsync();

        try
        {
            // get current user id from token
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new FailureResponseModel() { Detail = "User ID not found in token" });
            }
            if (!Guid.TryParse(userId, out _))
            {
                return BadRequest(new FailureResponseModel() { Detail = "You must provide a valid UUID" });
            }

            // check if new password value is same as old password value
            if (request.CurrentPassword == request.NewPassword)
            {
                return Conflict(new FailureResponseModel() { Detail = "New password may not be the same as the old password" });
            }

            // get user data from mariadb
            var user = await _mariaDb.QuerySingleOrDefaultAsync<UserRowStructure>(
                "SELECT * FROM users WHERE id = @userId",
                new { userId },
                transaction
            );
            if (user == null)
            {
                return NotFound(new FailureResponseModel() { Detail = "User not found" });
            }

            // verify current password
            if (!_passwordService.VerifyPassword(request.CurrentPassword, user.password_hash))
            {
                return BadRequest(new FailureResponseModel() { Detail = "Current password is incorrect" });
            }

            // hash new password
            var newPasswordHash = _passwordService.HashPassword(request.NewPassword);

            // store the new password hash in the database
            await _mariaDb.ExecuteAsync(
                """
                UPDATE users
                SET password_hash = @newPasswordHash
                WHERE id = @userId
                """,
                new
                {
                    newPasswordHash,
                    userId
                },
                transaction
            );
            await transaction.CommitAsync();

            // return
            return Ok(new SuccessResponseModel());
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Failed to change password");
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new FailureResponseModel() { Detail = $"Error changing password: {ex.Message}" }
            );
        }
    }
}
