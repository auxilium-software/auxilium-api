using AuxiliumAPI.Common.ControllerBases;
using AuxiliumAPI.Common.DataStructures.CouchDB;
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
public class MeController : LoggedInControllerBase
{
    private readonly IConfiguration Configuration;
    private readonly ILogger<MeController> _logger;
    private readonly ICouchDbService _couchDb;
    private readonly IMariaDbService _mariaDb;
    private readonly IPasswordService _passwordService;

    public MeController(
        IConfiguration configuration,
        ILogger<MeController> logger,
        ICouchDbService couchDb,
        IMariaDbService mariaDb,
        IPasswordService passwordService
        ) : base(mariaDb, logger)
    {
        this.Configuration = configuration;
        this._logger = logger;
        this._couchDb = couchDb;
        this._mariaDb = mariaDb;
        this._passwordService = passwordService;
    }

    [HttpGet("")]
    [ProducesResponseType(typeof(UserResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UserResponseModel>> GetDetailsAboutMyself()
    {
        try
        {
            // enforce login and get current user details
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            // get user document from couchdb
            var userDoc = await _couchDb.GetDocumentAsync<UserDocumentStructure>(
                this.Configuration!["Databases:CouchDB:Databases:Users"]!,
                user.id.ToString()
            );

            // if the user doc is not found, return not found
            if (userDoc == null)
            {
                return NotFound(new FailureResponseModel() { Detail = "User profile not found" });
            }

            // build response with both mariadb and couchdb data
            var response = new UserResponseModel
            {
                ID = user.id,
                CreatedAt = userDoc.CreatedAt,
                CreatedBy = userDoc.CreatedBy,
                LastUpdatedAt = userDoc.CreatedAt,
                LastUpdatedBy = userDoc.LastUpdatedBy,

                AdditionalProperties = userDoc.AdditionalProperties,
                Files = userDoc.Files,

                EmailAddress = user.email_address,
                IsAdmin = user.is_admin,
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
            // enforce login and get current user details
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            // check there is a file
            if (request.File == null || request.File.Length == 0)
            {
                return BadRequest(new FailureResponseModel() { Detail = "No file provided" });
            }

            // grab the user doc from couchdb
            var userDoc = await _couchDb.GetDocumentAsync<CaseDocumentStructure>(
                this.Configuration!["Databases:CouchDB:Databases:Users"]!,
                user.id.ToString()
            );
            if (userDoc == null)
            {
                return NotFound(new { detail = "Case not found" });
            }

            // read file contents
            using var memoryStream = new MemoryStream();
            await request.File.CopyToAsync(memoryStream);
            var fileBytes = memoryStream.ToArray();

            // get content type
            var contentType = request.File.ContentType ?? "application/octet-stream";

            // create file ID
            Guid fileId = UUIDUtilities.GenerateV5(DatabaseObjectType.File);

            // get file hash (SHA256)
            var fileHash = HashingUtilities.SHA256Hash(fileBytes);

            // create document for the file for couchdb
            var fileDoc = new FileDocumentStructure
            {
                Id = fileId.ToString(),
                CreatedBy = user.id,
                CreatedAt = DateTime.UtcNow,
                LastUpdatedBy = user.id,
                LastUpdatedAt = DateTime.UtcNow,
                Filename = request.File.FileName,
                Description = request.Description,
                ContentType = contentType,
                Hash = fileHash,
                Size = fileBytes.Length,
            };

            // update the case document to include a reference to the file
            userDoc.Files ??= new List<string>();
            userDoc.Files.Add($"auxlfs://localhost/file/{fileId}?size={fileBytes.Length}&hash={fileHash}");
            userDoc.LastUpdatedAt = DateTime.UtcNow;
            userDoc.LastUpdatedBy = user.id;

            // save file to lfs
            string path = this.Configuration!["FileSystem:RootStorageDirectories:AuxLFS"] + $"/{fileId}.bin";
            System.IO.File.WriteAllBytes(path, fileBytes);

            // save to couchdb
            await _couchDb.SaveDocumentAsync(
                this.Configuration!["Databases:CouchDB:Databases:Users"]!,
                userDoc
            );
            await _couchDb.SaveDocumentAsync(
                this.Configuration!["Databases:CouchDB:Databases:Files"]!,
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
            // enforce login and get current user details
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            // check if new password value is same as old password value
            if (request.CurrentPassword == request.NewPassword)
            {
                return Conflict(new FailureResponseModel() { Detail = "New password may not be the same as the old password" });
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
                    user.id
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
