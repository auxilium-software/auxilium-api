using AuxiliumSoftware.AuxiliumServices.API.Common.ControllerBases;
using AuxiliumSoftware.AuxiliumServices.API.Models;
using AuxiliumSoftware.AuxiliumServices.API.Models.AdditionalProperty;
using AuxiliumSoftware.AuxiliumServices.Common.Configuration;
using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework;
using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework.EntityModels;
using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework.Enumerators;
using AuxiliumSoftware.AuxiliumServices.Common.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace AuxiliumSoftware.AuxiliumServices.API.Controllers;

[ApiController]
[Route("/api/v3/users/{userId}/additional_properties")]
[Tags("Users")]
public class UserAdditionalPropertiesController : LoggedInControllerBase
{
    private readonly IUserDocumentService _userDocService;

    public UserAdditionalPropertiesController(
        IConfiguration configuration,
        AuxiliumDbContext db,
        ILogger<UserAdditionalPropertiesController> logger,
        ITotpService totpService,

        IUserDocumentService userDocService
        )
        : base(configuration, db, logger, totpService)
    {
        _userDocService = userDocService;
    }


    [HttpPost]
    [ProducesResponseType(typeof(SuccessResponseModel), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SuccessResponseModel>> CreateProperty(
        string userId,
        [FromBody] AdditionalPropertyCreationRequestModel request)
    {
        try
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            if (!Guid.TryParse(userId, out var userGuid))
            {
                return BadRequest(new FailureResponseModel { Detail = "Invalid user ID" });
            }

            // only allow admins or themselves to create additional properties
            if (!user!.IsAdmin && user.Id != userGuid)
            {
                return StatusCode(403, new FailureResponseModel
                {
                    Detail = "You can only modify your own properties"
                });
            }

            // verify the user actually exists
            var userDoc = await _userDocService.GetDocumentAsync(userGuid);
            if (userDoc == null)
            {
                return NotFound(new FailureResponseModel { Detail = "User not found" });
            }

            // save the additional property (service will throw exception if duplicate)
            await _userDocService.SaveAdditionalPropertyAsync(
                userGuid,
                request.Name,
                request.Content
            );

            this.Logger.LogInformation(
                "Created property {AdditionalPropertyName} for user {UserId} by user {CurrentUserId}",
                request.Name, userId, user.Id
            );

            return StatusCode(201, new SuccessResponseModel());
        }
        catch (Exception ex) when (ex.Message.Contains("already exists"))
        {
            return Conflict(new FailureResponseModel
            {
                Detail = "Property already exists. Use PATCH to update."
            });
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Failed to create property {AdditionalPropertyName} for user {UserId}", request.Name, userId);
            return StatusCode(500, new FailureResponseModel
            {
                Detail = "Failed to create property"
            });
        }
    }

    [HttpPatch("{additionalPropertyId}")]
    [ProducesResponseType(typeof(SuccessResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SuccessResponseModel>> UpdateProperty(
        string userId,
        string additionalPropertyId,
        [FromBody] AdditionalPropertyCreationRequestModel request)
    {
        try
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            if (!Guid.TryParse(userId, out var userGuid))
            {
                return BadRequest(new FailureResponseModel { Detail = "Invalid user ID" });
            }

            if (!Guid.TryParse(additionalPropertyId, out var additionalPropertyIdGuid))
            {
                return BadRequest(new FailureResponseModel { Detail = "Invalid additional property ID" });
            }

            // only allow admins or themselves to modify additional properties
            if (!user!.IsAdmin && user.Id != userGuid)
            {
                return StatusCode(403, new FailureResponseModel
                {
                    Detail = "You can only modify your own properties"
                });
            }

            // verify user exists
            var userDoc = await _userDocService.GetDocumentAsync(userGuid);
            if (userDoc == null)
            {
                return NotFound(new FailureResponseModel { Detail = "User not found" });
            }

            // get existing additional properties
            var properties = await _userDocService.GetAdditionalPropertiesAsync(userGuid);
            var existingProp = properties.FirstOrDefault(p => p.Id == additionalPropertyIdGuid);

            if (existingProp == null)
            {
                return NotFound(new FailureResponseModel
                {
                    Detail = "Property not found. Use POST to create."
                });
            }

            // update via EF Core directly
            //TODO: don't do this
            existingProp.Content = request.Content ?? string.Empty;
            existingProp.LastUpdatedAt = DateTime.UtcNow;
            existingProp.LastUpdatedBy = user!.Id;

            await Db.SaveChangesAsync();

            // update the LastUpdatedAt timestamp for the case
            var userEntity = await Db.Users.FindAsync(userGuid);
            if (userEntity != null)
            {
                userEntity.LastUpdatedAt = DateTime.UtcNow;
                await Db.SaveChangesAsync();
            }

            this.Logger.LogInformation(
                "Updated property {AdditionalPropertyName} for user {UserId} by user {CurrentUserId}",
                existingProp.Name, userId, user.Id
            );

            // return
            return Ok(new SuccessResponseModel());
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Failed to update property {AdditionalPropertyId} for user {UserId}", additionalPropertyId, userId);
            return StatusCode(500, new FailureResponseModel
            {
                Detail = "Failed to update property"
            });
        }
    }

    [HttpDelete("{additionalPropertyId}")]
    [ProducesResponseType(typeof(SuccessResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SuccessResponseModel>> DeleteProperty(
        string userId,
        string additionalPropertyId)
    {
        try
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            if (!Guid.TryParse(userId, out var userGuid))
            {
                return BadRequest(new FailureResponseModel { Detail = "Invalid user ID" });
            }

            if (!Guid.TryParse(additionalPropertyId, out var additionalPropertyIdGuid))
            {
                return BadRequest(new FailureResponseModel { Detail = "Invalid additional property ID" });
            }

            // only allow admins or themselves to delete additional properties
            if (!user!.IsAdmin && user.Id != userGuid)
            {
                return StatusCode(403, new FailureResponseModel
                {
                    Detail = "You can only modify your own properties"
                });
            }

            // verify user exists
            var userDoc = await _userDocService.GetDocumentAsync(userGuid);
            if (userDoc == null)
            {
                return NotFound(new FailureResponseModel { Detail = "User not found" });
            }

            // get existing properties
            var properties = await _userDocService.GetAdditionalPropertiesAsync(userGuid);
            var existingProp = properties.FirstOrDefault(p => p.Id == additionalPropertyIdGuid);

            if (existingProp == null)
            {
                return NotFound(new FailureResponseModel
                {
                    Detail = "Property not found"
                });
            }

            // delete by ID
            await _userDocService.DeleteAdditionalPropertyAsync(userGuid, existingProp.Id);

            this.Logger.LogInformation(
                "Deleted property {AdditionalPropertyName} from user {UserId} by user {CurrentUserId}",
                existingProp.Name, userId, user.Id
            );

            // return
            return Ok(new SuccessResponseModel());
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Failed to delete property {AdditionalPropertyId} from user {UserId}", additionalPropertyId, userId);
            return StatusCode(500, new FailureResponseModel
            {
                Detail = "Failed to delete property"
            });
        }
    }
}
