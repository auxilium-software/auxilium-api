using AuxiliumSoftware.AuxiliumServices.API.Common.ControllerBases;
using AuxiliumSoftware.AuxiliumServices.API.Common.Utilities;
using AuxiliumSoftware.AuxiliumServices.API.Models;
using AuxiliumSoftware.AuxiliumServices.API.Models.AdditionalProperty;
using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework;
using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework.EntityModels;
using AuxiliumSoftware.AuxiliumServices.Common.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuxiliumSoftware.AuxiliumServices.API.Controllers;

[ApiController]
[Route("/api/v3/cases/{caseId:guid}/additional_properties")]
[Tags("Cases")]
[Authorize]
public class SingleCaseAdditionalPropertiesController : LoggedInControllerBase
{
    private readonly ICaseDocumentService _caseDocService;

    public SingleCaseAdditionalPropertiesController(
        IConfiguration configuration,
        AuxiliumDbContext db,
        ILogger<SingleCaseAdditionalPropertiesController> logger,
        ITotpService totpService,
        ICaseDocumentService caseDocService)
        : base(configuration, db, logger, totpService)
    {
        _caseDocService = caseDocService;
    }

    private async Task<(CaseEntityModel? caseEntity, ActionResult? error)> GetCaseWithAccessCheckAsync(
        Guid caseId,
        UserEntityModel user,
        bool requireWorker = false)
    {
        var caseEntity = await Db.Cases
            .Include(c => c.Clients)
            .Include(c => c.Workers)
            .FirstOrDefaultAsync(c => c.Id == caseId);

        if (caseEntity == null)
        {
            return (null, NotFound(new FailureResponseModel { Detail = "Case not found" }));
        }

        var isWorker = (caseEntity.Workers ?? []).Any(w => w.UserId == user.Id);
        var isClient = (caseEntity.Clients ?? []).Any(c => c.UserId == user.Id);

        if (requireWorker)
        {
            if (!user.IsAdmin && !isWorker)
            {
                return (null, StatusCode(403, new FailureResponseModel
                {
                    Detail = "Only case workers can modify properties"
                }));
            }
        }
        else
        {
            if (!user.IsAdmin && !isWorker && !isClient)
            {
                return (null, StatusCode(403, new FailureResponseModel
                {
                    Detail = "You don't have permission to view this case"
                }));
            }
        }

        return (caseEntity, null);
    }

    [HttpGet("{propertyName}")]
    [ProducesResponseType(typeof(AdditionalPropertyResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdditionalPropertyResponseModel>> GetProperty(
        Guid caseId,
        string propertyName)
    {
        var (user, error) = await GetCurrentUserAsync();
        if (error != null) return error;

        var (caseEntity, caseError) = await GetCaseWithAccessCheckAsync(caseId, user!);
        if (caseError != null) return caseError;

        var properties = await _caseDocService.GetAdditionalPropertiesAsync(caseId);
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
        Guid caseId,
        string propertyName,
        [FromBody] AdditionalPropertyCreationRequestModel request)
    {
        try
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            var (caseEntity, caseError) = await GetCaseWithAccessCheckAsync(caseId, user!, requireWorker: true);
            if (caseError != null) return caseError;

            var sanitizedName = ControllerUtilities.SanitisePropertyName(request.OriginalName);
            if (string.IsNullOrEmpty(sanitizedName))
            {
                return BadRequest(new FailureResponseModel { Detail = "Invalid property name" });
            }

            var properties = await _caseDocService.GetAdditionalPropertiesAsync(caseId);
            var existingProp = properties.FirstOrDefault(p =>
                p.UrlSlug.Equals(sanitizedName, StringComparison.OrdinalIgnoreCase));

            if (existingProp != null)
            {
                return Conflict(new FailureResponseModel
                {
                    Detail = "Property already exists"
                });
            }

            await _caseDocService.SaveAdditionalPropertyAsync(
                user!,
                caseId,
                request.OriginalName,
                sanitizedName,
                request.Content,
                request.ContentType
            );

            Logger.LogInformation(
                "Created property {PropertyName} for case {CaseId} by {CurrentUserId}",
                sanitizedName, caseId, user!.Id);

            return StatusCode(201, new SuccessResponseModel());
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to create property for case {CaseId}", caseId);
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
        Guid caseId,
        string propertyName,
        [FromBody] AdditionalPropertyUpdateRequestModel request)
    {
        try
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            var (caseEntity, caseError) = await GetCaseWithAccessCheckAsync(caseId, user!, requireWorker: true);
            if (caseError != null) return caseError;

            var properties = await _caseDocService.GetAdditionalPropertiesAsync(caseId);
            var existingProp = properties.FirstOrDefault(p =>
                p.UrlSlug.Equals(propertyName, StringComparison.OrdinalIgnoreCase));

            if (existingProp == null)
            {
                return NotFound(new FailureResponseModel { Detail = "Property not found" });
            }

            existingProp.Content = request.Content;
            existingProp.ContentType = request.ContentType;
            existingProp.LastUpdatedAt = DateTime.UtcNow;
            existingProp.LastUpdatedBy = user!.Id;
            await Db.SaveChangesAsync();

            caseEntity!.LastUpdatedAt = DateTime.UtcNow;
            caseEntity.LastUpdatedBy = user.Id;
            await Db.SaveChangesAsync();

            Logger.LogInformation(
                "Updated property {PropertyName} for case {CaseId} by {CurrentUserId}",
                propertyName, caseId, user.Id);

            return Ok(new SuccessResponseModel());
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to update property {PropertyName} for case {CaseId}",
                propertyName, caseId);
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
        Guid caseId,
        string propertyName)
    {
        try
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            var (caseEntity, caseError) = await GetCaseWithAccessCheckAsync(caseId, user!, requireWorker: true);
            if (caseError != null) return caseError;

            var properties = await _caseDocService.GetAdditionalPropertiesAsync(caseId);
            var existingProp = properties.FirstOrDefault(p =>
                p.UrlSlug.Equals(propertyName, StringComparison.OrdinalIgnoreCase));

            if (existingProp == null)
            {
                return NotFound(new FailureResponseModel { Detail = "Property not found" });
            }

            await _caseDocService.DeleteAdditionalPropertyAsync(caseId, existingProp.Id);

            Logger.LogInformation(
                "Deleted property {PropertyName} from case {CaseId} by {CurrentUserId}",
                propertyName, caseId, user!.Id);

            return Ok(new SuccessResponseModel());
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to delete property {PropertyName} from case {CaseId}",
                propertyName, caseId);
            return StatusCode(500, new FailureResponseModel
            {
                Detail = "Failed to delete property"
            });
        }
    }
}
