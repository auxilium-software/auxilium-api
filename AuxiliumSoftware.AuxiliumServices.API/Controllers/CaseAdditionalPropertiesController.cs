using AuxiliumSoftware.AuxiliumServices.API.Models;
using AuxiliumSoftware.AuxiliumServices.API.Common.ControllerBases;
using AuxiliumSoftware.AuxiliumServices.API.Models.AdditionalProperty;
using AuxiliumSoftware.AuxiliumServices.Common.Configuration;
using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework;
using AuxiliumSoftware.AuxiliumServices.Common.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace AuxiliumSoftware.AuxiliumServices.API.Controllers;

[ApiController]
[Route("/api/v3/cases/{caseId}/additional_properties")]
[Tags("Cases")]
public class CaseAdditionalPropertiesController : LoggedInControllerBase
{
    private readonly ICaseDocumentService _caseDocService;

    public CaseAdditionalPropertiesController(
        IConfiguration configuration,
        AuxiliumDbContext db,
        ILogger<CaseAdditionalPropertiesController> logger,

        ICaseDocumentService caseDocService
        )
        : base(configuration, db, logger)
    {
        _caseDocService = caseDocService;
    }

    [HttpPost("")]
    [ProducesResponseType(typeof(SuccessResponseModel), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SuccessResponseModel>> CreateProperty(
        string caseId,
        [FromBody] AdditionalPropertyCreationRequestModel request)
    {
        try
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            if (!Guid.TryParse(caseId, out var caseGuid))
            {
                return BadRequest(new FailureResponseModel { Detail = "Invalid case ID" });
            }

            var caseDoc = await _caseDocService.GetDocumentAsync(caseGuid);
            if (caseDoc == null)
            {
                return NotFound(new FailureResponseModel { Detail = "Case not found" });
            }

            // only allow admins or case workers to create additional properties
            var isWorker = caseDoc.Workers?.Any(w => w.UserId == user!.Id) ?? false;
            if (!user!.IsAdmin && !isWorker)
            {
                return StatusCode(403, new FailureResponseModel
                {
                    Detail = "Only case workers and admins can modify case properties"
                });
            }

            // save the additional property (the service will throw exception if the given name already exists)
            await _caseDocService.SaveAdditionalPropertyAsync(
                caseGuid,
                request.Name,
                request.Content
            );

            this.Logger.LogInformation(
                "Created property {AdditionalPropertyName} for case {CaseId} by user {UserId}",
                request.Name, caseId, user.Id
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
            this.Logger.LogError(ex, "Failed to create property {AdditionalPropertyName} for case {CaseId}", request.Name, caseId);
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
        string caseId,
        string additionalPropertyId,
        [FromBody] AdditionalPropertyCreationRequestModel request)
    {
        try
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            if (!Guid.TryParse(caseId, out var caseGuid))
            {
                return BadRequest(new FailureResponseModel { Detail = "Invalid case ID" });
            }

            if (!Guid.TryParse(additionalPropertyId, out var additionalPropertyIdGuid))
            {
                return BadRequest(new FailureResponseModel { Detail = "Invalid additional property ID" });
            }

            var caseDoc = await _caseDocService.GetDocumentAsync(caseGuid);
            if (caseDoc == null)
            {
                return NotFound(new FailureResponseModel { Detail = "Case not found" });
            }

            // only allow admins or case workers to modify additional properties
            var isWorker = caseDoc.Workers?.Any(w => w.UserId == user!.Id) ?? false;
            if (!user!.IsAdmin && !isWorker)
            {
                return StatusCode(403, new FailureResponseModel
                {
                    Detail = "Only case workers and admins can modify case properties"
                });
            }

            // grab the existing additional properties
            var properties = await _caseDocService.GetAdditionalPropertiesAsync(caseGuid);
            var existingProp = properties.FirstOrDefault(p => p.Id == additionalPropertyIdGuid);

            if (existingProp == null)
            {
                return NotFound(new FailureResponseModel
                {
                    Detail = "Property not found. Use POST to create."
                });
            }

            // bit of a bodge to update EF directly
            //TODO: don't do this
            existingProp.Content = request.Content ?? string.Empty;
            existingProp.LastUpdatedAt = DateTime.UtcNow;
            existingProp.LastUpdatedBy = user!.Id;

            await Db.SaveChangesAsync();

            // update the LastUpdatedAt timestamp for the case
            var caseEntity = await Db.Cases.FindAsync(caseGuid);
            if (caseEntity != null)
            {
                caseEntity.LastUpdatedAt = DateTime.UtcNow;
                await Db.SaveChangesAsync();
            }

            this.Logger.LogInformation(
                "Updated property {AdditionalPropertyName} for case {CaseId} by user {UserId}",
                existingProp.Name, caseId, user.Id
            );

            return Ok(new SuccessResponseModel());
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Failed to update property {AdditionalPropertyId} for case {CaseId}", additionalPropertyId, caseId);
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
        string caseId,
        string additionalPropertyId
        )
    {
        try
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            if (!Guid.TryParse(caseId, out var caseGuid))
            {
                return BadRequest(new FailureResponseModel { Detail = "Invalid case ID" });
            }

            if (!Guid.TryParse(additionalPropertyId, out var additionalPropertyIdGuid))
            {
                return BadRequest(new FailureResponseModel { Detail = "Invalid additional property ID" });
            }

            var caseDoc = await _caseDocService.GetDocumentAsync(caseGuid);
            if (caseDoc == null)
            {
                return NotFound(new FailureResponseModel { Detail = "Case not found" });
            }

            // only allow admins or case workers to delete additional properties
            var isWorker = caseDoc.Workers?.Any(w => w.UserId == user!.Id) ?? false;
            if (!user!.IsAdmin && !isWorker)
            {
                return StatusCode(403, new FailureResponseModel
                {
                    Detail = "Only case workers and admins can modify case properties"
                });
            }

            // get existing additional properties
            var properties = await _caseDocService.GetAdditionalPropertiesAsync(caseGuid);
            var existingProp = properties.FirstOrDefault(p => p.Id == additionalPropertyIdGuid);

            if (existingProp == null)
            {
                return NotFound(new FailureResponseModel
                {
                    Detail = "Property not found"
                });
            }

            // delete the additional property by its id
            await _caseDocService.DeleteAdditionalPropertyAsync(caseGuid, existingProp.Id);

            this.Logger.LogInformation(
                "Deleted property {AdditionalPropertyName} from case {CaseId} by user {UserId}",
                existingProp.Name, caseId, user.Id
            );

            return Ok(new SuccessResponseModel());
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Failed to delete property {AdditionalPropertyId} from case {CaseId}", additionalPropertyId, caseId);
            return StatusCode(500, new FailureResponseModel
            {
                Detail = "Failed to delete property"
            });
        }
    }
}
