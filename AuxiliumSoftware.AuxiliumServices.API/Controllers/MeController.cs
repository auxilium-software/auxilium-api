using AuxiliumSoftware.AuxiliumServices.API.Common.ControllerBases;
using AuxiliumSoftware.AuxiliumServices.API.Models;
using AuxiliumSoftware.AuxiliumServices.API.Models.Me;
using AuxiliumSoftware.AuxiliumServices.API.Models.User;
using AuxiliumSoftware.AuxiliumServices.Common.Configuration;
using AuxiliumSoftware.AuxiliumServices.Common.DataTransferObjects;
using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework;
using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework.EntityModels;
using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework.Enumerators;
using AuxiliumSoftware.AuxiliumServices.Common.Services;
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
    private readonly IUserDocumentService _userDocService;
    private readonly IPasswordService _passwordService;

    public MeController(
        ISystemSettingsService systemSettingsService,
        IConfiguration configuration,
        AuxiliumDbContext db,
        IWebApplicationFirewallService waf,
        ILogger<MeController> logger,
        ITotpService totpService,

        IUserDocumentService userDocService,
        IPasswordService passwordService
        )
        : base(systemSettingsService, configuration, db, waf, logger, totpService)
    {
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
                return StatusCode(StatusCodes.Status404NotFound, new FailureResponseModel { Detail = "User profile not found" });
            }

            // get additional properties
            var additionalProperties = userDoc.AdditionalProperties?
                .ToDictionary(
                    p => p.UrlSlug,
                    p => new AdditionalPropertySubStructureDTO
                    {
                        Id = p.Id,
                        CreatedAt = p.CreatedAtUtc,
                        CreatedBy = p.CreatedByUserId,
                        UpdatedAt = p.LastUpdatedAtUtc,
                        LastUpdatedBy = p.LastUpdatedByUserId,
                        OriginalName = p.OriginalName,
                        UrlSlug = p.UrlSlug,
                        Content = p.Content,
                        ContentType = p.ContentType
                    }
                ) ?? new Dictionary<string, AdditionalPropertySubStructureDTO>();

            // build response
            var response = new UserResponseModel
            {
                ID = userDoc.Id,
                CreatedAt = userDoc.CreatedAtUtc,
                CreatedBy = userDoc.CreatedByUserId,
                LastUpdatedAt = userDoc.LastUpdatedAtUtc,
                LastUpdatedBy = userDoc.LastUpdatedByUserId,

                EmailAddress = userDoc.EmailAddress,
                IsAdministrator = userDoc.IsAdministrator,
                IsCaseWorkerManager = userDoc.IsCaseWorkerManager,
                IsCaseWorker = userDoc.IsCaseWorker,
                AllowLogin = userDoc.AllowLogin,
                IsEmailVerified = userDoc.HasEmailAddressBeenVerified,

                FullName = userDoc.FullName ?? string.Empty,
                FullAddress = userDoc.FullAddress ?? string.Empty,
                TelephoneNumber = userDoc.TelephoneNumber ?? string.Empty,
                Gender = userDoc.Gender ?? string.Empty,
                DateOfBirth = userDoc.DateOfBirth,
                LanguagePreference = userDoc.LanguagePreference ?? string.Empty,
                HowDidYouFindOutAboutOurService = userDoc.HowDidYouFindOutAboutOurService ?? string.Empty,

                AdditionalProperties = additionalProperties,
                Files = new List<string>() //TODO: sort this out
            };

            return StatusCode(StatusCodes.Status200OK, response);
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Failed to fetch user details for current user");
            return StatusCode(StatusCodes.Status500InternalServerError, new FailureResponseModel
            {
                Detail = "Failed to fetch user details"
            });
        }
    }

    [HttpPatch("")]
    [ProducesResponseType(typeof(SuccessResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status500InternalServerError)]
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
                return StatusCode(StatusCodes.Status404NotFound, new FailureResponseModel { Detail = "User not found" });
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

            if (request.LanguagePreference != null)
                userDoc.LanguagePreference = request.LanguagePreference;

            userDoc.LastUpdatedAtUtc = DateTime.UtcNow;
            userDoc.LastUpdatedByUserId = user!.Id;

            await Db.SaveChangesAsync();

            this.Logger.LogInformation("User {UserId} updated their profile", user.Id);

            return StatusCode(StatusCodes.Status200OK, new SuccessResponseModel());
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Failed to update user profile");
            return StatusCode(StatusCodes.Status500InternalServerError, new FailureResponseModel
            {
                Detail = "Failed to update profile"
            });
        }
    }

    [HttpPost("change-password")]
    [ProducesResponseType(typeof(SuccessResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SuccessResponseModel>> ChangePassword(
        [FromBody] PasswordUpdateRequestModel request
    )
    {
        try
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            var currentNormalized = this._passwordService.NormalisePassword(request.CurrentPasswordSha512, request.CurrentPasswordSha512);
            var newNormalized = this._passwordService.NormalisePassword(request.NewPasswordSha512, request.NewPasswordSha512);

            if (currentNormalized == newNormalized)
            {
                return StatusCode(StatusCodes.Status409Conflict, new FailureResponseModel
                {
                    Detail = "New password may not be the same as the old password"
                });
            }

            var userDoc = await Db.Users.FirstOrDefaultAsync(u => u.Id == user!.Id);
            if (userDoc == null)
            {
                return StatusCode(StatusCodes.Status404NotFound, new FailureResponseModel
                {
                    Detail = "User not found"
                });
            }

            if (!_passwordService.VerifyPassword(currentNormalized, userDoc.PasswordHash))
            {
                return StatusCode(StatusCodes.Status400BadRequest, new FailureResponseModel
                {
                    Detail = "Current password is incorrect"
                });
            }

            var newPasswordHash = _passwordService.HashPassword(newNormalized);

            userDoc.PasswordHash = newPasswordHash;
            userDoc.LastUpdatedAtUtc = DateTime.UtcNow;

            await Db.SaveChangesAsync();

            this.Logger.LogInformation("User {UserId} changed their password", user!.Id);

            return StatusCode(StatusCodes.Status200OK, new SuccessResponseModel());
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Failed to change password");
            return StatusCode(StatusCodes.Status500InternalServerError, new FailureResponseModel
            {
                Detail = "Error changing password"
            });
        }
    }

    [HttpPost("request-account-deletion")]
    [ProducesResponseType(typeof(SuccessResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SuccessResponseModel>> RequestAccountDeletion(
        [FromBody] RequestMyAccountDeletionRequestModel request
    )
    {
        try
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            var currentNormalized = this._passwordService.NormalisePassword(request.CurrentPasswordSha512, request.CurrentPasswordSha512);

            var userDoc = await Db.Users.FirstOrDefaultAsync(u => u.Id == user!.Id);
            if (userDoc == null)
            {
                return StatusCode(StatusCodes.Status400BadRequest, new FailureResponseModel
                {
                    Detail = "User not found"
                });
            }

            if (!_passwordService.VerifyPassword(currentNormalized, userDoc.PasswordHash))
            {
                return StatusCode(StatusCodes.Status400BadRequest, new FailureResponseModel
                {
                    Detail = "Current password is incorrect"
                });
            }

            userDoc.DeletionRequested = true;
            userDoc.DeletionRequestReason = request.Reason;

            await Db.SaveChangesAsync();

            this.Logger.LogInformation("User {UserId} requested their account to be deleted", user!.Id);

            return StatusCode(StatusCodes.Status200OK, new SuccessResponseModel());
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Failed to request to delete account");
            return StatusCode(StatusCodes.Status500InternalServerError, new FailureResponseModel
            {
                Detail = "Error requesting account deletion"
            });
        }
    }
}
