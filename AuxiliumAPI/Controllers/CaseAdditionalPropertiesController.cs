using AuxiliumAPI.Common.ControllerBases;
using AuxiliumAPI.Common.DataStructures.CouchDB.SubStructures;
using AuxiliumAPI.Common.Services.Interfaces;
using AuxiliumAPI.Common.Utilities;
using AuxiliumAPI.Models;
using AuxiliumAPI.Models.AdditionalProperty;
using Microsoft.AspNetCore.Mvc;

namespace AuxiliumAPI.Controllers;

[ApiController]
[Route("api/v3/cases/{caseId}/additional_properties")]
[Tags("Cases")]
public class CasePropertiesController : LoggedInControllerBase
{
    private readonly ICaseDocumentService _caseDocService;
    private readonly ILogger<CasePropertiesController> _logger;

    public CasePropertiesController(
        ICaseDocumentService caseDocService,
        ILogger<CasePropertiesController> logger,
        IMariaDbService mariaDb)
        : base(mariaDb, logger)
    {
        _caseDocService = caseDocService;
        _logger = logger;
    }

    [HttpPost("{propertyName}")]
    [ProducesResponseType(typeof(SuccessResponseModel), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SuccessResponseModel>> CreateProperty(
        string caseId,
        string propertyName,
        [FromBody] AdditionalPropertyCreationRequestModel request)
    {
        try
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            // check to make sure that the given case id is a valid uuid
            if (!Guid.TryParse(caseId, out _))
            {
                return BadRequest(new FailureResponseModel { Detail = "Invalid case ID" });
            }

            // grab the case doc
            var caseDoc = await _caseDocService.GetDocumentAsync(Guid.Parse(caseId));
            if (caseDoc == null)
            {
                return NotFound(new FailureResponseModel { Detail = "Case not found" });
            }

            // only allow admins or case workers to create additional properties
            if (!user!.is_admin && !caseDoc.Workers.Contains(user.id))
            {
                return StatusCode(403, new FailureResponseModel
                {
                    Detail = "Only case workers and admins can modify case properties"
                });
            }

            // normalise property names
            var (storageKey, autoDisplayName) = PropertyNameHandlingUtilities.HandlePropertyName(propertyName);
            var finalDisplayName = request.DisplayName ?? autoDisplayName;

            // gran existing additional properties
            var properties = await _caseDocService.GetAdditionalPropertiesAsync(Guid.Parse(caseId));

            // check if the property already exists
            if (properties.ContainsKey(storageKey))
            {
                return Conflict(new FailureResponseModel
                {
                    Detail = "Property already exists. Use PATCH to update."
                });
            }

            // create the substructure to save into the case document
            var propVal = new AdditionalPropertySubStructure()
            {
                Content = request.Content ?? string.Empty,
                ContentType = request.ContentType ?? "Text",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = user.id,
                Id = Guid.NewGuid(),
                OriginalName = propertyName,
                PrettyName = finalDisplayName,
                UrlSlug = PropertyNameHandlingUtilities.NormalizeKey(finalDisplayName)
            };

            // save the additional property
            await _caseDocService.SaveAdditionalPropertyAsync(Guid.Parse(caseId), storageKey, propVal);

            _logger.LogInformation(
                "Created property {PropertyName} for case {CaseId} by user {UserId}",
                storageKey, caseId, user.id
            );

            // return
            return StatusCode(201, new SuccessResponseModel());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create property {PropertyName} for case {CaseId}", propertyName, caseId);
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
        string caseId,
        string propertyName,
        [FromBody] AdditionalPropertyCreationRequestModel request)
    {
        try
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            // make sure that the given case id is a valid uuid
            if (!Guid.TryParse(caseId, out _))
            {
                return BadRequest(new FailureResponseModel { Detail = "Invalid case ID" });
            }

            // grab the case doc
            var caseDoc = await _caseDocService.GetDocumentAsync(Guid.Parse(caseId));
            if (caseDoc == null)
            {
                return NotFound(new FailureResponseModel { Detail = "Case not found" });
            }

            // only allow admins or case workers to modify additional properties
            if (!user!.is_admin && !caseDoc.Workers.Contains(user.id))
            {
                return StatusCode(403, new FailureResponseModel
                {
                    Detail = "Only case workers and admins can modify case properties"
                });
            }

            // normalise property names
            var (storageKey, autoDisplayName) = PropertyNameHandlingUtilities.HandlePropertyName(propertyName);

            // grab the existing additional properties
            var properties = await _caseDocService.GetAdditionalPropertiesAsync(Guid.Parse(caseId));

            // check if the additional property actually exists
            if (!properties.ContainsKey(storageKey))
            {
                return NotFound(new FailureResponseModel
                {
                    Detail = "Property not found. Use POST to create."
                });
            }

            // update the additional property
            var finalDisplayName = request.DisplayName ?? autoDisplayName;

            // create the substructure to save into the user document
            var propVal = new AdditionalPropertySubStructure()
            {
                Content = request.Content ?? string.Empty,
                ContentType = request.ContentType ?? "Text",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = user.id,
                Id = Guid.NewGuid(),
                OriginalName = propertyName,
                PrettyName = finalDisplayName,
                UrlSlug = PropertyNameHandlingUtilities.NormalizeKey(finalDisplayName)
            };

            // save the additional property
            await _caseDocService.SaveAdditionalPropertyAsync(
                Guid.Parse(caseId),
                storageKey,
                propVal
            );

            _logger.LogInformation(
                "Updated property {PropertyName} for case {CaseId} by user {UserId}",
                storageKey, caseId, user.id
            );

            // return
            return Ok(new SuccessResponseModel());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update property {PropertyName} for case {CaseId}", propertyName, caseId);
            return StatusCode(500, new FailureResponseModel
            {
                Detail = "Failed to update property"
            });
        }
    }

    [HttpDelete("{propertyName}")]
    [ProducesResponseType(typeof(SuccessResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SuccessResponseModel>> DeleteProperty(
        string caseId,
        string propertyName
        )
    {
        try
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            // check to make sure that the given user id is a valid uuid
            if (!Guid.TryParse(caseId, out _))
            {
                return BadRequest(new FailureResponseModel { Detail = "Invalid case ID" });
            }

            // grab the case doc
            var caseDoc = await _caseDocService.GetDocumentAsync(Guid.Parse(caseId));
            if (caseDoc == null)
            {
                return NotFound(new FailureResponseModel { Detail = "Case not found" });
            }

            // only allow admins or case workers to delete additional properties
            if (!user!.is_admin && !caseDoc.Workers.Contains(user.id))
            {
                return StatusCode(403, new FailureResponseModel
                {
                    Detail = "Only case workers and admins can modify case properties"
                });
            }

            // grab the key for the given additional property name
            var (storageKey, _) = PropertyNameHandlingUtilities.HandlePropertyName(propertyName);

            // grab all the existing additional properties for this user
            var properties = await _caseDocService.GetAdditionalPropertiesAsync(Guid.Parse(caseId));

            // make sure the additional property actually exists
            if (!properties.ContainsKey(storageKey))
            {
                return NotFound(new FailureResponseModel
                {
                    Detail = "Property not found"
                });
            }

            // delete the additional property
            await _caseDocService.DeleteAdditionalPropertyAsync(Guid.Parse(caseId), storageKey);

            _logger.LogInformation(
                "Deleted property {PropertyName} from case {CaseId} by user {UserId}",
                storageKey, caseId, user.id
            );

            // return
            return Ok(new SuccessResponseModel());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete property {PropertyName} from case {CaseId}", propertyName, caseId);
            return StatusCode(500, new FailureResponseModel
            {
                Detail = "Failed to delete property"
            });
        }
    }
}
