using AuxiliumSoftware.AuxiliumServices.API.Common.ControllerBases;
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
    private readonly IDataEnumeratorService _dataEnumeratorService;

    public SingleUserAdditionalPropertiesController(
        ISystemSettingsService systemSettingsService,
        IConfiguration configuration,
        AuxiliumDbContext db,
        IWebApplicationFirewallService waf,
        ILogger<SingleUserAdditionalPropertiesController> logger,
        ITotpService totpService,

        IUserDocumentService userDocService,
        IDataEnumeratorService dataEnumeratorService
    )
        : base(systemSettingsService, configuration, db, waf, logger, totpService)
    {
        _userDocService = userDocService;
        _dataEnumeratorService = dataEnumeratorService;
    }

    [HttpGet("{propertyId:guid}")]
    [ProducesResponseType(typeof(AdditionalPropertyResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdditionalPropertyResponseModel>> GetProperty(
        Guid userId,
        Guid propertyId)
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
        var property = properties.FirstOrDefault(p => p.Id == propertyId);

        if (property == null)
        {
            return StatusCode(StatusCodes.Status404NotFound, new FailureResponseModel { Detail = "Property not found" });
        }

        var response = new AdditionalPropertyResponseModel
        {
            Id = property.Id,
            DisplayName = property.DisplayName,
            Content = property.Content,
            ContentType = property.ContentType,
            CreatedAt = property.CreatedAtUtc,
            LastUpdatedAt = property.LastUpdatedAtUtc
        };

        if (property.ContentType == Common.Constants.DataEnumeratorReferenceContentType
            && Guid.TryParse(property.Content, out var valueId))
        {
            var resolved = await _dataEnumeratorService.ResolveValueDisplaysAsync(
                [valueId], user!.LanguagePreference);
            if (resolved.TryGetValue(valueId, out var r))
            {
                response.DataEnumeratorId = r.EnumTypeId;
                response.DisplayValue = r.ValueDisplay;
                response.EnumDisplayName = r.EnumDisplay;
            }
        }

        return StatusCode(StatusCodes.Status200OK, response);
    }

    [HttpPost]
    [ProducesResponseType(typeof(AdditionalPropertyCreationResponseModel), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AdditionalPropertyCreationResponseModel>> CreateProperty(
        Guid userId,
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

            var effectiveName = request.OriginalName;

            if (string.IsNullOrWhiteSpace(effectiveName)
                && request.ContentType == Common.Constants.DataEnumeratorReferenceContentType)
            {
                if (!Guid.TryParse(request.Content, out var valueId))
                    return StatusCode(StatusCodes.Status400BadRequest,
                        new FailureResponseModel { Detail = "Invalid enumerator value reference" });

                var resolved = await _dataEnumeratorService.ResolveValueDisplaysAsync([valueId], null);
                if (!resolved.TryGetValue(valueId, out var r))
                    return StatusCode(StatusCodes.Status400BadRequest,
                        new FailureResponseModel { Detail = "Enumerator value not found" });

                effectiveName = r.EnumCanonicalName;
            }

            if (string.IsNullOrWhiteSpace(effectiveName))
            {
                return StatusCode(StatusCodes.Status400BadRequest,
                    new FailureResponseModel { Detail = "Property name is required" });
            }

            var newId = await _userDocService.SaveAdditionalPropertyAsync(
                user,
                userId,
                effectiveName,
                request.Content,
                request.ContentType
            );
            await this._userDocService.WriteToAuditLog(
                actorUserId: user.Id,
                targetUserId: userDoc.Id,
                entityType: UserEntityTypeEnum.User_AdditionalProperty,
                actionType: AuditLogActionTypeEnum.Creation,
                entityId: newId
            );
            await this.Db.SaveChangesAsync();

            Logger.LogInformation(
                "Created property {PropertyId} ({Name}) for user {UserId} by {CurrentUserId}",
                newId, effectiveName, userId, user.Id);

            return StatusCode(StatusCodes.Status201Created,
                new AdditionalPropertyCreationResponseModel { Id = newId });
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

    [HttpPatch("{propertyId:guid}")]
    [ProducesResponseType(typeof(SuccessResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SuccessResponseModel>> UpdateProperty(
        Guid userId,
        Guid propertyId,
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
            var existingProp = properties.FirstOrDefault(p => p.Id == propertyId);

            if (existingProp == null)
            {
                return StatusCode(StatusCodes.Status404NotFound, new FailureResponseModel { Detail = "Property not found" });
            }

            var previousContent = existingProp.Content;

            existingProp.Content = request.Content;
            existingProp.ContentType = request.ContentType;
            existingProp.LastUpdatedAtUtc = DateTime.UtcNow;
            existingProp.LastUpdatedByUserId = user.Id;

            var userEntity = await Db.Users.FindAsync(userId);
            if (userEntity != null)
            {
                userEntity.LastUpdatedAtUtc = DateTime.UtcNow;
            }

            await _userDocService.WriteToAuditLog(
                actorUserId: user.Id,
                targetUserId: userDoc.Id,
                entityType: UserEntityTypeEnum.User_AdditionalProperty,
                entityId: existingProp.Id,
                actionType: AuditLogActionTypeEnum.Modification,
                propertyName: "content",
                oldValue: previousContent,
                newValue: existingProp.Content
            );
            await Db.SaveChangesAsync();

            Logger.LogInformation(
                "Updated property {PropertyId} ({Name}) for user {UserId} by {CurrentUserId}",
                existingProp.Id, existingProp.DisplayName, userId, user.Id);

            return StatusCode(StatusCodes.Status200OK, new SuccessResponseModel());
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to update property {PropertyId} for user {UserId}",
                propertyId, userId);
            return StatusCode(StatusCodes.Status500InternalServerError, new FailureResponseModel
            {
                Detail = "Failed to update property"
            });
        }
    }

    [HttpDelete("{propertyId:guid}")]
    [ProducesResponseType(typeof(SuccessResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SuccessResponseModel>> DeleteProperty(
        Guid userId,
        Guid propertyId)
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
            var existingProp = properties.FirstOrDefault(p => p.Id == propertyId);

            if (existingProp == null)
            {
                return StatusCode(StatusCodes.Status404NotFound, new FailureResponseModel { Detail = "Property not found" });
            }

            await _userDocService.DeleteAdditionalPropertyAsync(
                userId: userId,
                additionalPropertyId: existingProp.Id,
                actorUserId: user.Id
            );

            await this._userDocService.WriteToAuditLog(
                actorUserId: user.Id,
                targetUserId: userDoc.Id,
                entityType: UserEntityTypeEnum.User_AdditionalProperty,
                actionType: AuditLogActionTypeEnum.Deletion,
                entityId: existingProp.Id
            );
            await this.Db.SaveChangesAsync();

            Logger.LogInformation(
                "Deleted property {PropertyId} from user {UserId} by {CurrentUserId}",
                existingProp.Id, userId, user!.Id
            );

            return StatusCode(StatusCodes.Status200OK, new SuccessResponseModel());
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to delete property {PropertyId} from user {UserId}",
                propertyId, userId);
            return StatusCode(StatusCodes.Status500InternalServerError, new FailureResponseModel
            {
                Detail = "Failed to delete property"
            });
        }
    }
}