using AuxiliumSoftware.AuxiliumServices.API.Common.ControllerBases;
using AuxiliumSoftware.AuxiliumServices.API.Models;
using AuxiliumSoftware.AuxiliumServices.API.Models.Case;
using AuxiliumSoftware.AuxiliumServices.API.Models.CaseTimeline;
using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework;
using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework.Enumerators;
using AuxiliumSoftware.AuxiliumServices.Common.Services;
using Microsoft.AspNetCore.Mvc;

namespace AuxiliumSoftware.AuxiliumServices.API.Controllers
{
    [ApiController]
    [Route("/api/v3/cases/{caseId:guid}/timeline")]
    [Tags("Cases")]
    public class SingleCaseTimelineController : LoggedInControllerBase
    {
        private readonly ICaseDocumentService _caseDocService;

        public SingleCaseTimelineController(
            ISystemSettingsService systemSettingsService,
            IConfiguration configuration,
            AuxiliumDbContext db,
            IWebApplicationFirewallService waf,
            ILogger<SingleCaseTimelineController> logger,
            ITotpService totpService,

            ICaseDocumentService caseDocService
            )
            : base(systemSettingsService, configuration, db, waf, logger, totpService)
        {
            _caseDocService = caseDocService;
        }

        [HttpPost("")]
        [ProducesResponseType(typeof(CaseTimelineEntryResponseModel), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<CaseTimelineEntryResponseModel>> CreateTimelineNote(
            Guid caseId,
            [FromBody] CaseTimelineEntryCreationRequestModel request
        )
        {
            try
            {
                var (user, error) = await GetCurrentUserAsync();
                if (error != null) return error;

                // check the user's access to the case
                if (!await _caseDocService.CheckUserAccessAsync(caseId, user!))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new FailureResponseModel
                    {
                        Detail = "You don't have permission to add timeline entries to this case"
                    });
                }

                var caseDoc = await _caseDocService.GetDocumentAsync(caseId);
                if (
                    caseDoc == null
                    || caseDoc.Workers == null
                    || !caseDoc.Workers.Any(w => w.UserId == user.Id)
                    || user.IsAdministrator
                )
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new FailureResponseModel
                    {
                        Detail = "Only case workers assigned to this case can add timeline entries"
                    });
                }

                // create the timeline entry
                var entry = await _caseDocService.CreateTimelineEntryAsync(
                    caseId: caseId,
                    title: request.Title,
                    occuredAt: request.OccurredAtUtc,
                    description: request.Description,
                    createdBy: user!.Id
                );

                await Db.SaveChangesAsync();

                return StatusCode(StatusCodes.Status201Created, new CaseTimelineEntryResponseModel
                {
                    Id = entry.Id,
                    CaseId = caseId,
                    Title = entry.Title,
                    Description = entry.Description,
                    OccurredAtUtc = entry.OccurredAtUtc,
                    CreatedAt = entry.CreatedAtUtc,
                    CreatedBy = entry.CreatedByUserId
                });
            }
            catch (KeyNotFoundException ex)
            {
                this.Logger.LogWarning(ex, "Case not found: {CaseId}", caseId);
                return StatusCode(StatusCodes.Status404NotFound, new FailureResponseModel { Detail = "Case not found" });
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex, "Failed to create timeline entry in case {CaseId}", caseId);
                return StatusCode(StatusCodes.Status500InternalServerError, new FailureResponseModel { Detail = "Failed to create timeline entry" });
            }
        }

        [HttpPatch("{timelineEntryId:guid}")]
        [ProducesResponseType(typeof(SuccessResponseModel), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<SuccessResponseModel>> UpdateTimelineNote(
            Guid caseId,
            Guid timelineEntryId,
            [FromBody] CaseTimelineEntryUpdateRequestModel request
        )
        {
            try
            {
                var (user, error) = await GetCurrentUserAsync();
                if (error != null) return error;

                // check the user's access to the case
                if (!await _caseDocService.CheckUserAccessAsync(caseId, user!))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new FailureResponseModel
                    {
                        Detail = "You don't have permission to update timeline entries for this case"
                    });
                }

                var caseDoc = await _caseDocService.GetDocumentAsync(caseId);
                if (
                    caseDoc == null
                    || caseDoc.Workers == null
                    || !caseDoc.Workers.Any(w => w.UserId == user.Id)
                    || user.IsAdministrator
                )
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new FailureResponseModel
                    {
                        Detail = "Only case workers assigned to this case can add timeline entries"
                    });
                }

                // update the timeline entry
                await _caseDocService.UpdateTimelineEntryAsync(
                    caseId: caseId,
                    timelineEntryId: timelineEntryId,
                    occuredAt: request.OccurredAtUtc,
                    updatedBy: user.Id,
                    title: request.Title,
                    description: request.Description
                );
                await Db.SaveChangesAsync();

                return StatusCode(StatusCodes.Status200OK, new SuccessResponseModel());
            }
            catch (KeyNotFoundException)
            {
                return StatusCode(StatusCodes.Status404NotFound, new FailureResponseModel { Detail = "Timeline entry not found" });
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex, "Failed to update timeline entry {TimelineEntryId}", timelineEntryId);
                return StatusCode(StatusCodes.Status500InternalServerError, new FailureResponseModel { Detail = "Failed to update timeline entry" });
            }
        }

        [HttpDelete("{timelineEntryId:guid}")]
        [ProducesResponseType(typeof(SuccessResponseModel), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<SuccessResponseModel>> DeleteTimelineEntry(
            Guid caseId,
            Guid timelineEntryId
        )
        {
            try
            {
                var (user, error) = await GetCurrentUserAsync();
                if (error != null) return error;

                // check the user's access to the case
                var caseDoc = await _caseDocService.GetDocumentAsync(caseId);
                if (caseDoc == null)
                {
                    return StatusCode(StatusCodes.Status404NotFound, new FailureResponseModel { Detail = "Case not found" });
                }

                // only admins and case workers can delete timeline entries
                var isWorker = caseDoc.Workers?.Any(w => w.UserId == user!.Id) ?? false;
                if (!user!.IsAdministrator && !isWorker)
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new FailureResponseModel
                    {
                        Detail = "Only case workers and admins can delete timeline entries"
                    });
                }

                // delete the timeline entry
                await _caseDocService.DeleteTimelineEntryAsync(
                    caseId: caseId,
                    timelineEntryId: timelineEntryId,
                    actorUserId: user.Id
                );

                return StatusCode(StatusCodes.Status200OK, new SuccessResponseModel());
            }
            catch (KeyNotFoundException)
            {
                return StatusCode(StatusCodes.Status404NotFound, new FailureResponseModel { Detail = "Timeline entry not found" });
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex, "Failed to delete timeline entry {TimelineEntryId}", timelineEntryId);
                return StatusCode(StatusCodes.Status500InternalServerError, new FailureResponseModel { Detail = "Failed to delete timeline entry" });
            }
        }
    }
}
