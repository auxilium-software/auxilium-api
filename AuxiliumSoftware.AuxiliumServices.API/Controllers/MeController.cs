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
                return NotFound(new FailureResponseModel { Detail = "User profile not found" });
            }

            // get additional properties
            var additionalProperties = userDoc.AdditionalProperties?
                .ToDictionary(
                    p => p.UrlSlug,
                    p => new AdditionalPropertySubStructureDTO
                    {
                        Id = p.Id,
                        CreatedAt = p.CreatedAt,
                        CreatedBy = p.CreatedBy,
                        UpdatedAt = p.LastUpdatedAt,
                        LastUpdatedBy = p.LastUpdatedBy,
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
                CreatedAt = userDoc.CreatedAt,
                CreatedBy = userDoc.CreatedBy,
                LastUpdatedAt = userDoc.LastUpdatedAt,
                LastUpdatedBy = userDoc.LastUpdatedBy,

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
                Files = new List<string>() // Files list from file service if needed
            };

            // return
            return Ok(response);
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Failed to fetch user details for current user");
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

            this.Logger.LogInformation("User {UserId} updated their profile", user.Id);

            return Ok(new SuccessResponseModel());
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Failed to update user profile");
            return StatusCode(500, new FailureResponseModel
            {
                Detail = "Failed to update profile"
            });
        }
    }

    [HttpPost("change-password")]
    public async Task<ActionResult<SuccessResponseModel>> ChangePassword(
        [FromBody] PasswordUpdateRequestModel request)
    {
        try
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            var currentNormalized = this._passwordService.NormalisePassword(request.CurrentPasswordSha512, request.CurrentPasswordSha512);
            var newNormalized = this._passwordService.NormalisePassword(request.NewPasswordSha512, request.NewPasswordSha512);

            if (currentNormalized == newNormalized)
            {
                return Conflict(new FailureResponseModel
                {
                    Detail = "New password may not be the same as the old password"
                });
            }

            var userDoc = await Db.Users.FirstOrDefaultAsync(u => u.Id == user!.Id);
            if (userDoc == null)
            {
                return NotFound(new FailureResponseModel { Detail = "User not found" });
            }

            if (!_passwordService.VerifyPassword(currentNormalized, userDoc.PasswordHash))
            {
                return BadRequest(new FailureResponseModel
                {
                    Detail = "Current password is incorrect"
                });
            }

            var newPasswordHash = _passwordService.HashPassword(newNormalized);

            userDoc.PasswordHash = newPasswordHash;
            userDoc.LastUpdatedAt = DateTime.UtcNow;

            await Db.SaveChangesAsync();

            this.Logger.LogInformation("User {UserId} changed their password", user!.Id);

            return Ok(new SuccessResponseModel());
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Failed to change password");
            return StatusCode(500, new FailureResponseModel
            {
                Detail = "Error changing password"
            });
        }
    }
}
