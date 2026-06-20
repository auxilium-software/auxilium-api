using AuxiliumSoftware.AuxiliumServices.API.Common.ControllerBases;
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
    private readonly IDataEnumeratorService _dataEnumeratorService;

    public SingleCaseAdditionalPropertiesController(
        ISystemSettingsService systemSettingsService,
        IConfiguration configuration,
        AuxiliumDbContext db,
        IWebApplicationFirewallService waf,
        ILogger<SingleCaseAdditionalPropertiesController> logger,
        ITotpService totpService,

        ICaseDocumentService caseDocService,
        IDataEnumeratorService dataEnumeratorService
    )
        : base(systemSettingsService, configuration, db, waf, logger, totpService)
    {
        _caseDocService = caseDocService;
        _dataEnumeratorService = dataEnumeratorService;
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
            return (null, StatusCode(StatusCodes.Status404NotFound, new FailureResponseModel { Detail = "Case not found" }));
        }

        var isWorker = (caseEntity.Workers ?? []).Any(w => w.UserId == user.Id);
        var isClient = (caseEntity.Clients ?? []).Any(c => c.UserId == user.Id);

        if (requireWorker)
        {
            if (!user.IsAdministrator && !isWorker)
            {
                return (null, StatusCode(StatusCodes.Status403Forbidden, new FailureResponseModel
                {
                    Detail = "Only case workers can modify properties"
                }));
            }
        }
        else
        {
            if (!user.IsAdministrator && !isWorker && !isClient)
            {
                return (null, StatusCode(StatusCodes.Status403Forbidden, new FailureResponseModel
                {
                    Detail = "You don't have permission to view this case"
                }));
            }
        }

        return (caseEntity, null);
    }

    [HttpGet("{propertyId:guid}")]
    [ProducesResponseType(typeof(AdditionalPropertyResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdditionalPropertyResponseModel>> GetProperty(
        Guid caseId,
        Guid propertyId)
    {
        var (user, error) = await GetCurrentUserAsync();
        if (error != null) return error;

        var (caseEntity, caseError) = await GetCaseWithAccessCheckAsync(caseId, user!);
        if (caseError != null) return caseError;

        var properties = await _caseDocService.GetAdditionalPropertiesAsync(caseId);
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
        Guid caseId,
        [FromBody] AdditionalPropertyCreationRequestModel request)
    {
        try
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            var (caseEntity, caseError) = await GetCaseWithAccessCheckAsync(caseId, user!, requireWorker: true);
            if (caseError != null) return caseError;

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

            var newId = await _caseDocService.SaveAdditionalPropertyAsync(
                user!,
                caseId,
                effectiveName,
                request.Content,
                request.ContentType
            );

            Logger.LogInformation(
                "Created property {PropertyId} ({Name}) for case {CaseId} by {CurrentUserId}",
                newId, effectiveName, caseId, user!.Id);

            return StatusCode(StatusCodes.Status201Created,
                new AdditionalPropertyCreationResponseModel { Id = newId });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to create property for case {CaseId}", caseId);
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
        Guid caseId,
        Guid propertyId,
        [FromBody] AdditionalPropertyUpdateRequestModel request)
    {
        try
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            var (caseEntity, caseError) = await GetCaseWithAccessCheckAsync(caseId, user!, requireWorker: true);
            if (caseError != null) return caseError;

            var properties = await _caseDocService.GetAdditionalPropertiesAsync(caseId);
            var existingProp = properties.FirstOrDefault(p => p.Id == propertyId);

            if (existingProp == null)
            {
                return StatusCode(StatusCodes.Status404NotFound, new FailureResponseModel { Detail = "Property not found" });
            }

            existingProp.Content = request.Content;
            existingProp.ContentType = request.ContentType;
            existingProp.LastUpdatedAtUtc = DateTime.UtcNow;
            existingProp.LastUpdatedByUserId = user!.Id;
            await Db.SaveChangesAsync();

            caseEntity!.LastUpdatedAtUtc = DateTime.UtcNow;
            caseEntity.LastUpdatedByUserId = user.Id;
            await Db.SaveChangesAsync();

            Logger.LogInformation(
                "Updated property {PropertyId} ({Name}) for case {CaseId} by {CurrentUserId}",
                existingProp.Id, existingProp.DisplayName, caseId, user.Id);

            return StatusCode(StatusCodes.Status200OK, new SuccessResponseModel());
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to update property {PropertyId} for case {CaseId}",
                propertyId, caseId);
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
        Guid caseId,
        Guid propertyId)
    {
        try
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            var (caseEntity, caseError) = await GetCaseWithAccessCheckAsync(caseId, user!, requireWorker: true);
            if (caseError != null) return caseError;

            var properties = await _caseDocService.GetAdditionalPropertiesAsync(caseId);
            var existingProp = properties.FirstOrDefault(p => p.Id == propertyId);

            if (existingProp == null)
            {
                return StatusCode(StatusCodes.Status404NotFound, new FailureResponseModel { Detail = "Property not found" });
            }

            await _caseDocService.DeleteAdditionalPropertyAsync(caseId, existingProp.Id);

            Logger.LogInformation(
                "Deleted property {PropertyId} from case {CaseId} by {CurrentUserId}",
                existingProp.Id, caseId, user!.Id);

            return StatusCode(StatusCodes.Status200OK, new SuccessResponseModel());
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to delete property {PropertyId} from case {CaseId}",
                propertyId, caseId);
            return StatusCode(StatusCodes.Status500InternalServerError, new FailureResponseModel
            {
                Detail = "Failed to delete property"
            });
        }
    }
}
