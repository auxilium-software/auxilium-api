using AuxiliumSoftware.AuxiliumServices.API.Common.ControllerBases;
using AuxiliumSoftware.AuxiliumServices.API.Models;
using AuxiliumSoftware.AuxiliumServices.API.Models.Me;
using AuxiliumSoftware.AuxiliumServices.API.Models.User;
using AuxiliumSoftware.AuxiliumServices.Common.Configuration;
using AuxiliumSoftware.AuxiliumServices.Common.DataStructures;
using AuxiliumSoftware.AuxiliumServices.Common.EF;
using AuxiliumSoftware.AuxiliumServices.Common.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuxiliumSoftware.AuxiliumServices.API.Controllers;

[ApiController]
[Route("/api/v3/me")]
[Tags("Account Management")]
[Authorize]
public class MeController : LoggedInControllerBase
{
    private readonly ILogger<MeController> _logger;

    private readonly IUserDocumentService _userDocService;
    private readonly IPasswordService _passwordService;

    public MeController(
        ILogger<MeController> logger,

        IUserDocumentService userDocService,
        IPasswordService passwordService,
        AuxiliumDbContext db
        )
        : base(db, logger)
    {
        _logger = logger;

        _userDocService = userDocService;
        _passwordService = passwordService;
    }

    [HttpGet("")]
    [ProducesResponseType(typeof(UserResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UserResponseModel>> GetDetailsAboutMyself()
    {
        try
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            // get user with additional properties
            var userDoc = await Db.Users
                .Include(u => u.AdditionalProperties)
                .FirstOrDefaultAsync(u => u.Id == user!.Id);

            if (userDoc == null)
            {
                return NotFound(new FailureResponseModel { Detail = "User profile not found" });
            }

            // get additional properties
            var additionalProperties = userDoc.AdditionalProperties?
                .ToDictionary(
                    p => p.Name,
                    p => new AdditionalPropertySubStructure
                    {
                        Id = p.Id,
                        CreatedAt = p.CreatedAt,
                        CreatedBy = p.CreatedBy,
                        UpdatedAt = p.LastUpdatedAt,
                        LastUpdatedBy = p.LastUpdatedBy,
                        OriginalName = p.Name,
                        PrettyName = p.Name,
                        UrlSlug = p.Name.ToLower().Replace(" ", "-"),
                        Content = p.Content,
                        ContentType = p.ContentType
                    }
                ) ?? new Dictionary<string, AdditionalPropertySubStructure>();

            // build response
            var response = new UserResponseModel
            {
                ID = userDoc.Id,
                CreatedAt = userDoc.CreatedAt,
                CreatedBy = userDoc.CreatedBy,
                LastUpdatedAt = userDoc.LastUpdatedAt,
                LastUpdatedBy = userDoc.LastUpdatedBy,

                EmailAddress = userDoc.EmailAddress,
                IsAdmin = userDoc.IsAdmin,
                IsCaseWorker = userDoc.IsCaseWorker,

                FullName = userDoc.FullName ?? string.Empty,
                FullAddress = userDoc.FullAddress ?? string.Empty,
                TelephoneNumber = userDoc.TelephoneNumber ?? string.Empty,
                Gender = userDoc.Gender ?? string.Empty,
                DateOfBirth = userDoc.DateOfBirth,
                HowDidYouFindOutAboutOurService = userDoc.HowDidYouFindOutAboutOurService ?? string.Empty,

                AdditionalProperties = additionalProperties,
                Files = new List<string>() // Files list from file service if needed
            };

            // return
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch user details for current user");
            return StatusCode(500, new FailureResponseModel
            {
                Detail = "Failed to fetch user details"
            });
        }
    }

    [HttpPatch("")]
    [ProducesResponseType(typeof(SuccessResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SuccessResponseModel>> UpdateMyProfile(
        [FromBody] ProfileUpdateRequestModel request)
    {
        try
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            var userDoc = await Db.Users.FirstOrDefaultAsync(u => u.Id == user!.Id);
            if (userDoc == null)
            {
                return NotFound(new FailureResponseModel { Detail = "User not found" });
            }

            // update fields if provided
            if (request.FullName != null)
                userDoc.FullName = request.FullName;

            if (request.FullAddress != null)
                userDoc.FullAddress = request.FullAddress;

            if (request.TelephoneNumber != null)
                userDoc.TelephoneNumber = request.TelephoneNumber;

            if (request.Gender != null)
                userDoc.Gender = request.Gender;

            if (request.DateOfBirth.HasValue)
                userDoc.DateOfBirth = request.DateOfBirth.Value;

            if (request.HowDidYouFindOutAboutOurService != null)
                userDoc.HowDidYouFindOutAboutOurService = request.HowDidYouFindOutAboutOurService;

            userDoc.LastUpdatedAt = DateTime.UtcNow;
            userDoc.LastUpdatedBy = user!.Id;

            await Db.SaveChangesAsync();

            _logger.LogInformation("User {UserId} updated their profile", user.Id);

            return Ok(new SuccessResponseModel());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update user profile");
            return StatusCode(500, new FailureResponseModel
            {
                Detail = "Failed to update profile"
            });
        }
    }

    [HttpPost("change-password")]
    [ProducesResponseType(typeof(SuccessResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SuccessResponseModel>> ChangePassword(
        [FromBody] PasswordUpdateRequestModel request)
    {
        try
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            // check if the new password is same as the old password
            if (request.CurrentPassword == request.NewPassword)
            {
                return Conflict(new FailureResponseModel
                {
                    Detail = "New password may not be the same as the old password"
                });
            }

            // grab the user from database
            var userDoc = await Db.Users.FirstOrDefaultAsync(u => u.Id == user!.Id);
            if (userDoc == null)
            {
                return NotFound(new FailureResponseModel { Detail = "User not found" });
            }

            // verify current password
            if (!_passwordService.VerifyPassword(request.CurrentPassword, userDoc.PasswordHash))
            {
                return BadRequest(new FailureResponseModel
                {
                    Detail = "Current password is incorrect"
                });
            }

            // hash new password
            var newPasswordHash = _passwordService.HashPassword(request.NewPassword);

            // update password
            userDoc.PasswordHash = newPasswordHash;
            userDoc.LastUpdatedAt = DateTime.UtcNow;

            await Db.SaveChangesAsync();

            _logger.LogInformation("User {UserId} changed their password", user!.Id);

            return Ok(new SuccessResponseModel());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to change password");
            return StatusCode(500, new FailureResponseModel
            {
                Detail = "Error changing password"
            });
        }
    }
}
