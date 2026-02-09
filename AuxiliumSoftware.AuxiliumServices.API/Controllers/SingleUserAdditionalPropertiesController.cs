using AuxiliumSoftware.AuxiliumServices.API.Common.ControllerBases;
using AuxiliumSoftware.AuxiliumServices.API.Common.Utilities;
using AuxiliumSoftware.AuxiliumServices.API.Models;
using AuxiliumSoftware.AuxiliumServices.API.Models.AdditionalProperty;
using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework;
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
        IWafService waf,
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

        if (!user!.IsAdmin && user.Id != userId)
        {
            return StatusCode(403, new FailureResponseModel
            {
                Detail = "You can only view your own properties"
            });
        }

        var userDoc = await _userDocService.GetDocumentAsync(userId);
        if (userDoc == null)
        {
            return NotFound(new FailureResponseModel { Detail = "User not found" });
        }

        var properties = await _userDocService.GetAdditionalPropertiesAsync(userId);
        var property = properties.FirstOrDefault(p =>
            p.UrlSlug.Equals(propertyName, StringComparison.OrdinalIgnoreCase));

        if (property == null)
        {
            return NotFound(new FailureResponseModel { Detail = "Property not found" });
        }

        return Ok(new AdditionalPropertyResponseModel
        {
            UrlSlug = property.UrlSlug,
            OriginalName = property.OriginalName,
            Content = property.Content,
            ContentType = property.ContentType,
            CreatedAt = property.CreatedAt,
            LastUpdatedAt = property.LastUpdatedAt
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

            if (!user!.IsAdmin && user.Id != userId)
            {
                return StatusCode(403, new FailureResponseModel
                {
                    Detail = "You can only modify your own properties"
                });
            }

            var userDoc = await _userDocService.GetDocumentAsync(userId);
            if (userDoc == null)
            {
                return NotFound(new FailureResponseModel { Detail = "User not found" });
            }

            var sanitizedName = ControllerUtilities.SanitisePropertyName(request.OriginalName);
            if (string.IsNullOrEmpty(sanitizedName))
            {
                return BadRequest(new FailureResponseModel { Detail = "Invalid property name" });
            }

            var properties = await _userDocService.GetAdditionalPropertiesAsync(userId);
            var existingProp = properties.FirstOrDefault(p =>
                p.UrlSlug.Equals(sanitizedName, StringComparison.OrdinalIgnoreCase));

            if (existingProp != null)
            {
                return Conflict(new FailureResponseModel
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

            return StatusCode(201, new SuccessResponseModel());
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to create property for user {UserId}", userId);
            return StatusCode(500, new FailureResponseModel
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

            if (!user!.IsAdmin && user.Id != userId)
            {
                return StatusCode(403, new FailureResponseModel
                {
                    Detail = "You can only modify your own properties"
                });
            }

            var userDoc = await _userDocService.GetDocumentAsync(userId);
            if (userDoc == null)
            {
                return NotFound(new FailureResponseModel { Detail = "User not found" });
            }

            var properties = await _userDocService.GetAdditionalPropertiesAsync(userId);
            var existingProp = properties.FirstOrDefault(p =>
                p.UrlSlug.Equals(propertyName, StringComparison.OrdinalIgnoreCase));

            if (existingProp == null)
            {
                return NotFound(new FailureResponseModel { Detail = "Property not found" });
            }

            existingProp.Content = request.Content;
            existingProp.ContentType = request.ContentType;
            existingProp.LastUpdatedAt = DateTime.UtcNow;
            existingProp.LastUpdatedBy = user.Id;
            await Db.SaveChangesAsync();

            var userEntity = await Db.Users.FindAsync(userId);
            if (userEntity != null)
            {
                userEntity.LastUpdatedAt = DateTime.UtcNow;
                await Db.SaveChangesAsync();
            }

            Logger.LogInformation(
                "Updated property {PropertyName} for user {UserId} by {CurrentUserId}",
                propertyName, userId, user.Id);

            return Ok(new SuccessResponseModel());
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to update property {PropertyName} for user {UserId}",
                propertyName, userId);
            return StatusCode(500, new FailureResponseModel
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

            if (!user!.IsAdmin && user.Id != userId)
            {
                return StatusCode(403, new FailureResponseModel
                {
                    Detail = "You can only modify your own properties"
                });
            }

            var userDoc = await _userDocService.GetDocumentAsync(userId);
            if (userDoc == null)
            {
                return NotFound(new FailureResponseModel { Detail = "User not found" });
            }

            var properties = await _userDocService.GetAdditionalPropertiesAsync(userId);
            var existingProp = properties.FirstOrDefault(p =>
                p.UrlSlug.Equals(propertyName, StringComparison.OrdinalIgnoreCase));

            if (existingProp == null)
            {
                return NotFound(new FailureResponseModel { Detail = "Property not found" });
            }

            await _userDocService.DeleteAdditionalPropertyAsync(userId, existingProp.Id);

            Logger.LogInformation(
                "Deleted property {PropertyName} from user {UserId} by {CurrentUserId}",
                propertyName, userId, user.Id);

            return Ok(new SuccessResponseModel());
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to delete property {PropertyName} from user {UserId}",
                propertyName, userId);
            return StatusCode(500, new FailureResponseModel
            {
                Detail = "Failed to delete property"
            });
        }
    }
}
