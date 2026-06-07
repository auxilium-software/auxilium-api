using AuxiliumSoftware.AuxiliumServices.API.Common.ControllerBases;
using AuxiliumSoftware.AuxiliumServices.API.Common.Utilities;
using AuxiliumSoftware.AuxiliumServices.API.Models;
using AuxiliumSoftware.AuxiliumServices.API.Models.AdditionalProperty;
using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework;
using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework.Enumerators;
using AuxiliumSoftware.AuxiliumServices.Common.Services;
using Microsoft.AspNetCore.Mvc;

namespace AuxiliumSoftware.AuxiliumServices.API.Controllers;

[ApiController]
[Route("/api/v3/users/{userId:guid}/additional_properties")]
[Tags("Users")]
public class SingleUserAdditionalPropertiesController : LoggedInControllerBase
{
    private readonly IUserDocumentService _userDocService;

    public SingleUserAdditionalPropertiesController(
        ISystemSettingsService systemSettingsService,
        IConfiguration configuration,
        AuxiliumDbContext db,
        IWebApplicationFirewallService waf,
        ILogger<SingleUserAdditionalPropertiesController> logger,
        ITotpService totpService,

        IUserDocumentService userDocService
    )
        : base(systemSettingsService, configuration, db, waf, logger, totpService)
    {
        _userDocService = userDocService;
    }

    [HttpGet("{propertyName}")]
    [ProducesResponseType(typeof(AdditionalPropertyResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdditionalPropertyResponseModel>> GetProperty(
        Guid userId,
        string propertyName)
    {
        var (user, error) = await GetCurrentUserAsync();
        if (error != null) return error;

        if (!user!.IsAdministrator && user.Id != userId)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new FailureResponseModel
            {
                Detail = "You can only view your own properties"
            });
        }

        var userDoc = await _userDocService.GetDocumentAsync(userId);
        if (userDoc == null)
        {
            return StatusCode(StatusCodes.Status404NotFound, new FailureResponseModel { Detail = "User not found" });
        }

        var properties = await _userDocService.GetAdditionalPropertiesAsync(userId);
        var property = properties.FirstOrDefault(p =>
            p.UrlSlug.Equals(propertyName, StringComparison.OrdinalIgnoreCase));

        if (property == null)
        {
            return StatusCode(StatusCodes.Status404NotFound, new FailureResponseModel { Detail = "Property not found" });
        }

        return StatusCode(StatusCodes.Status200OK, new AdditionalPropertyResponseModel
        {
            UrlSlug = property.UrlSlug,
            OriginalName = property.OriginalName,
            Content = property.Content,
            ContentType = property.ContentType,
            CreatedAt = property.CreatedAtUtc,
            LastUpdatedAt = property.LastUpdatedAtUtc
        });
    }

    [HttpPost("{propertyName}")]
    [ProducesResponseType(typeof(SuccessResponseModel), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SuccessResponseModel>> CreateProperty(
        Guid userId,
        string propertyName,
        [FromBody] AdditionalPropertyCreationRequestModel request)
    {
        try
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            if (!user!.IsAdministrator && user.Id != userId)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new FailureResponseModel
                {
                    Detail = "You can only modify your own properties"
                });
            }

            var userDoc = await _userDocService.GetDocumentAsync(userId);
            if (userDoc == null)
            {
                return StatusCode(StatusCodes.Status404NotFound, new FailureResponseModel { Detail = "User not found" });
            }

            var sanitizedName = ControllerUtilities.SanitisePropertyName(request.OriginalName);
            if (string.IsNullOrEmpty(sanitizedName))
            {
                return StatusCode(StatusCodes.Status400BadRequest, new FailureResponseModel { Detail = "Invalid property name" });
            }

            var properties = await _userDocService.GetAdditionalPropertiesAsync(userId);
            var existingProp = properties.FirstOrDefault(p =>
                p.UrlSlug.Equals(sanitizedName, StringComparison.OrdinalIgnoreCase));

            if (existingProp != null)
            {
                return StatusCode(StatusCodes.Status409Conflict, new FailureResponseModel
                {
                    Detail = "Property already exists"
                });
            }

            await _userDocService.SaveAdditionalPropertyAsync(
                user,
                userId,
                request.OriginalName,
                sanitizedName,
                request.Content,
                request.ContentType
            );

            Logger.LogInformation(
                "Created property {PropertyName} for user {UserId} by {CurrentUserId}",
                sanitizedName, userId, user.Id);

            return StatusCode(StatusCodes.Status201Created, new SuccessResponseModel());
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to create property for user {UserId}", userId);
            return StatusCode(StatusCodes.Status500InternalServerError, new FailureResponseModel
            {
                Detail = "Failed to create property"
            });
        }
    }

    [HttpPatch("{propertyName}")]
    [ProducesResponseType(typeof(SuccessResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SuccessResponseModel>> UpdateProperty(
        Guid userId,
        string propertyName,
        [FromBody] AdditionalPropertyUpdateRequestModel request)
    {
        try
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            if (!user!.IsAdministrator && user.Id != userId)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new FailureResponseModel
                {
                    Detail = "You can only modify your own properties"
                });
            }

            var userDoc = await _userDocService.GetDocumentAsync(userId);
            if (userDoc == null)
            {
                return StatusCode(StatusCodes.Status404NotFound, new FailureResponseModel { Detail = "User not found" });
            }

            var properties = await _userDocService.GetAdditionalPropertiesAsync(userId);
            var existingProp = properties.FirstOrDefault(p =>
                p.UrlSlug.Equals(propertyName, StringComparison.OrdinalIgnoreCase));

            if (existingProp == null)
            {
                return StatusCode(StatusCodes.Status404NotFound, new FailureResponseModel { Detail = "Property not found" });
            }

            var previousContent = existingProp.Content;
            
            existingProp.Content = request.Content;
            existingProp.ContentType = request.ContentType;
            existingProp.LastUpdatedAtUtc = DateTime.UtcNow;
            existingProp.LastUpdatedBy = user.Id;

            var userEntity = await Db.Users.FindAsync(userId);
            if (userEntity != null)
            {
                userEntity.LastUpdatedAtUtc = DateTime.UtcNow;
            }

            await _userDocService.WriteToAuditLog(
                currentUser: user,
                targetUser: userDoc,
                entityType: UserEntityTypeEnum.User_AdditionalProperty,
                entityId: existingProp.Id,
                actionType: AuditLogActionTypeEnum.Modification,
                propertyName: propertyName,
                oldValue: previousContent,
                newValue: existingProp.Content
            );
            await Db.SaveChangesAsync();

            Logger.LogInformation(
                "Updated property {PropertyName} for user {UserId} by {CurrentUserId}",
                propertyName, userId, user.Id);

            return StatusCode(StatusCodes.Status200OK, new SuccessResponseModel());
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to update property {PropertyName} for user {UserId}",
                propertyName, userId);
            return StatusCode(StatusCodes.Status500InternalServerError, new FailureResponseModel
            {
                Detail = "Failed to update property"
            });
        }
    }

    [HttpDelete("{propertyName}")]
    [ProducesResponseType(typeof(SuccessResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SuccessResponseModel>> DeleteProperty(
        Guid userId,
        string propertyName)
    {
        try
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            if (!user!.IsAdministrator && user.Id != userId)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new FailureResponseModel
                {
                    Detail = "You can only modify your own properties"
                });
            }

            var userDoc = await _userDocService.GetDocumentAsync(userId);
            if (userDoc == null)
            {
                return StatusCode(StatusCodes.Status404NotFound, new FailureResponseModel { Detail = "User not found" });
            }

            var properties = await _userDocService.GetAdditionalPropertiesAsync(userId);
            var existingProp = properties.FirstOrDefault(p =>
                p.UrlSlug.Equals(propertyName, StringComparison.OrdinalIgnoreCase));

            if (existingProp == null)
            {
                return StatusCode(StatusCodes.Status404NotFound, new FailureResponseModel { Detail = "Property not found" });
            }

            await _userDocService.DeleteAdditionalPropertyAsync(userId, existingProp.Id);

            Logger.LogInformation(
                "Deleted property {PropertyName} from user {UserId} by {CurrentUserId}",
                propertyName, userId, user!.Id);

            return StatusCode(StatusCodes.Status200OK, new SuccessResponseModel());
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to delete property {PropertyName} from user {UserId}",
                propertyName, userId);
            return StatusCode(StatusCodes.Status500InternalServerError, new FailureResponseModel
            {
                Detail = "Failed to delete property"
            });
        }
    }
}
