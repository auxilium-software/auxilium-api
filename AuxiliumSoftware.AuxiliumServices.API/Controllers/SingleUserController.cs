using AuxiliumSoftware.AuxiliumServices.API.Common.ControllerBases;
using AuxiliumSoftware.AuxiliumServices.API.Common.Utilities;
using AuxiliumSoftware.AuxiliumServices.API.Models;
using AuxiliumSoftware.AuxiliumServices.API.Models.User;
using AuxiliumSoftware.AuxiliumServices.API.Models.UserStatistic;
using AuxiliumSoftware.AuxiliumServices.Common.Configuration;
using AuxiliumSoftware.AuxiliumServices.Common.DataStructures;
using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework;
using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework.EntityModels;
using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework.Enumerators;
using AuxiliumSoftware.AuxiliumServices.Common.Services;
using AuxiliumSoftware.AuxiliumServices.Common.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Channels;

namespace AuxiliumSoftware.AuxiliumServices.API.Controllers;

[ApiController]
[Route("/api/v3/users/{userId:guid}")]
[Tags("Users")]
public class SingleUserController : LoggedInControllerBase
{
    private readonly IUserDocumentService _userDocService;
    private readonly ITotpService _totpService;

    public SingleUserController(
        ISystemSettingsService systemSettingsService,
        IConfiguration configuration,
        AuxiliumDbContext db,
        ILogger<SingleUserController> logger,
        ITotpService totpService,

        IUserDocumentService userDocService
        )
        : base(systemSettingsService, configuration, db, logger, totpService)
    {
        _userDocService = userDocService;
        _totpService = totpService;
    }


    [HttpGet("")]
    [ProducesResponseType(typeof(UserResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UserResponseModel>> GetUserById(Guid userId)
    {
        try
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            UserEntityModel targetUser = Db.Users.FirstOrDefault(u => u.Id == userId);
            if(targetUser == null)
                return StatusCode(StatusCodes.Status404NotFound, new FailureResponseModel
                {
                    Detail = "The targeted User does not exist."
                });

            if (await ControllerUtilities.HasUserGotConnectionToUser(Db, user!, targetUser) == false)
                return StatusCode(StatusCodes.Status403Forbidden, new FailureResponseModel
                {
                    Detail = "You may not access this user."
                });

            var userDoc = await Db.Users
                .Include(u => u.AdditionalProperties)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (userDoc == null)
            {
                return NotFound(new FailureResponseModel { Detail = "User not found" });
            }

            return Ok(ControllerUtilities.UserMapToUserResponseModel(userDoc, user!.IsAdmin));
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Failed to fetch user {UserId}", userId);
            return StatusCode(500, new FailureResponseModel
            {
                Detail = "Failed to fetch user"
            });
        }
    }


    [HttpPatch("")]
    [ProducesResponseType(typeof(UserResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UserResponseModel>> UpdateUser(
        Guid userId,
        [FromBody] UpdateUserRequestModel request
    )
    {
        try
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            UserEntityModel targetUser = Db.Users.FirstOrDefault(u => u.Id == userId);
            if (targetUser == null)
                return StatusCode(StatusCodes.Status404NotFound, new FailureResponseModel
                {
                    Detail = "The targeted User does not exist."
                });

            if (await ControllerUtilities.HasUserGotConnectionToUser(Db, user!, targetUser) == false)
                return StatusCode(StatusCodes.Status403Forbidden, new FailureResponseModel
                {
                    Detail = "You may not access this user."
                });

            var totpError = await RequireTotpAsync(user!.Id);
            if (totpError != null) return totpError;

            var userDoc = await Db.Users
                .Include(u => u.AdditionalProperties)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (userDoc == null)
            {
                return NotFound(new FailureResponseModel { Detail = "User not found" });
            }

            // apply changes only for fields that were provided
            if (request.FullName != null && request.FullName != userDoc.FullName)
            {
                var oldValue = userDoc.FullName;
                userDoc.FullName = request.FullName;
                _userDocService.WriteToAuditLog(user, userDoc, UserEntityTypeEnum.User, userDoc.Id,
                    AuditLogActionTypeEnum.Modification, "FullName", oldValue, request.FullName);
            }

            if (request.EmailAddress != null && request.EmailAddress != userDoc.EmailAddress)
            {
                // check for email uniqueness
                var emailExists = await Db.Users
                    .AnyAsync(u => u.Id != userId && u.EmailAddress == request.EmailAddress);

                if (emailExists)
                {
                    return BadRequest(new FailureResponseModel
                    {
                        Detail = "A user with this email address already exists"
                    });
                }

                var oldValue = userDoc.EmailAddress;
                userDoc.EmailAddress = request.EmailAddress;
                _userDocService.WriteToAuditLog(user, userDoc, UserEntityTypeEnum.User, userDoc.Id,
                    AuditLogActionTypeEnum.Modification, "EmailAddress", oldValue, request.EmailAddress);
            }

            if (request.TelephoneNumber != null && request.TelephoneNumber != userDoc.TelephoneNumber)
            {
                var oldValue = userDoc.TelephoneNumber;
                userDoc.TelephoneNumber = request.TelephoneNumber;
                _userDocService.WriteToAuditLog(user, userDoc, UserEntityTypeEnum.User, userDoc.Id,
                    AuditLogActionTypeEnum.Modification, "TelephoneNumber", oldValue, request.TelephoneNumber);
            }

            if (request.FullAddress != null && request.FullAddress != userDoc.FullAddress)
            {
                var oldValue = userDoc.FullAddress;
                userDoc.FullAddress = request.FullAddress;
                _userDocService.WriteToAuditLog(user, userDoc, UserEntityTypeEnum.User, userDoc.Id,
                    AuditLogActionTypeEnum.Modification, "FullAddress", oldValue, request.FullAddress);
            }

            if (request.Gender != null && request.Gender != userDoc.Gender)
            {
                var oldValue = userDoc.Gender;
                userDoc.Gender = request.Gender;
                _userDocService.WriteToAuditLog(user, userDoc, UserEntityTypeEnum.User, userDoc.Id,
                    AuditLogActionTypeEnum.Modification, "Gender", oldValue, request.Gender);
            }

            if (request.DateOfBirth.HasValue && request.DateOfBirth.Value != userDoc.DateOfBirth)
            {
                var oldValue = userDoc.DateOfBirth?.ToString("yyyy-MM-dd");
                userDoc.DateOfBirth = request.DateOfBirth.Value;
                _userDocService.WriteToAuditLog(user, userDoc, UserEntityTypeEnum.User, userDoc.Id,
                    AuditLogActionTypeEnum.Modification, "DateOfBirth", oldValue, request.DateOfBirth.Value.ToString("yyyy-MM-dd"));
            }

            if (request.LanguagePreference != null && request.LanguagePreference != userDoc.LanguagePreference)
            {
                var oldValue = userDoc.LanguagePreference;
                userDoc.LanguagePreference = request.LanguagePreference;
                _userDocService.WriteToAuditLog(user, userDoc, UserEntityTypeEnum.User, userDoc.Id,
                    AuditLogActionTypeEnum.Modification, "LanguagePreference", oldValue, request.LanguagePreference);
            }

            if (request.HowDidYouFindOutAboutOurService != null && request.HowDidYouFindOutAboutOurService != userDoc.HowDidYouFindOutAboutOurService)
            {
                var oldValue = userDoc.HowDidYouFindOutAboutOurService;
                userDoc.HowDidYouFindOutAboutOurService = request.HowDidYouFindOutAboutOurService;
                _userDocService.WriteToAuditLog(user, userDoc, UserEntityTypeEnum.User, userDoc.Id,
                    AuditLogActionTypeEnum.Modification, "HowDidYouFindOutAboutOurService", oldValue, request.HowDidYouFindOutAboutOurService);
            }

            userDoc.LastUpdatedAt = DateTime.UtcNow;
            userDoc.LastUpdatedBy = user!.Id;

            await Db.SaveChangesAsync();

            Logger.LogInformation(
                "User {UserId} updated by admin {AdminId}",
                userId, user.Id
            );

            return Ok(ControllerUtilities.UserMapToUserResponseModel(userDoc, true));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to update user {UserId}", userId);
            return StatusCode(500, new FailureResponseModel
            {
                Detail = "Failed to update user"
            });
        }
    }





    [HttpDelete("")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> DeleteUser(Guid userId)
    {
        try
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            var adminError = await RequireAdminAsync();
            if (adminError != null) return adminError;

            var totpError = await RequireTotpAsync(user!.Id);
            if (totpError != null) return totpError;

            // prevent self-deletion
            if (userId == user!.Id)
            {
                return BadRequest(new FailureResponseModel
                {
                    Detail = "You cannot delete your own account"
                });
            }

            var userDoc = await Db.Users
                .Include(u => u.AdditionalProperties)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (userDoc == null)
            {
                return NotFound(new FailureResponseModel { Detail = "User not found" });
            }

            // remove additional properties first
            if (userDoc.AdditionalProperties != null && userDoc.AdditionalProperties.Any())
            {
                Db.RemoveRange(userDoc.AdditionalProperties);
            }

            Db.Users.Remove(userDoc);

            this._userDocService.WriteToAuditLog(
                currentUser: user,
                targetUser: userDoc,
                entityType: UserEntityTypeEnum.User,
                entityId: userDoc.Id,
                actionType: AuditLogActionTypeEnum.Deletion
            );

            await Db.SaveChangesAsync();

            Logger.LogWarning(
                "User {UserId} ({Email}) deleted by admin {AdminId}",
                userId, userDoc.EmailAddress, user.Id
            );

            return NoContent();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to delete user {UserId}", userId);
            return StatusCode(500, new FailureResponseModel
            {
                Detail = "Failed to delete user"
            });
        }
    }




}
