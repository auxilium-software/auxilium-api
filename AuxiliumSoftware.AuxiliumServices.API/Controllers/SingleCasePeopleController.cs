using AuxiliumSoftware.AuxiliumServices.API.Common.ControllerBases;
using AuxiliumSoftware.AuxiliumServices.API.Models;
using AuxiliumSoftware.AuxiliumServices.API.Models.Case;
using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework;
using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework.EntityModels;
using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework.Enumerators;
using AuxiliumSoftware.AuxiliumServices.Common.Services;
using Microsoft.AspNetCore.Mvc;

namespace AuxiliumSoftware.AuxiliumServices.API.Controllers;

[ApiController]
[Route("/api/v3/cases/{caseId:guid}")]
[Tags("Cases")]
public class SingleCasePeopleController : LoggedInControllerBase
{
    private readonly ICaseDocumentService _caseDocService;

    public SingleCasePeopleController(
        ISystemSettingsService systemSettingsService,
        IConfiguration configuration,
        AuxiliumDbContext db,
        IWebApplicationFirewallService waf,
        ILogger<SingleCasePeopleController> logger,
        ITotpService totpService,

        ICaseDocumentService caseDocService
        )
        : base(systemSettingsService, configuration, db, waf, logger, totpService)
    {
        _caseDocService = caseDocService;
    }

    #region ========================= CLIENTS =========================
    [HttpPost("clients")]
    [ProducesResponseType(typeof(SuccessResponseModel), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SuccessResponseModel>> AddClientToCase(
        Guid caseId,
        [FromBody] AddPersonRequestModel request
    )
    {
        try
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            // grab the case with workers
            var caseDoc = await _caseDocService.GetDocumentAsync(caseId);
            if (caseDoc == null)
            {
                return StatusCode(StatusCodes.Status404NotFound, new FailureResponseModel { Detail = "Case not found" });
            }

            // only admins or existing workers can add clients
            var isWorker = caseDoc.Workers?.Any(w => w.UserId == user!.Id) ?? false;
            if (!user!.IsAdministrator && !isWorker)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new FailureResponseModel
                {
                    Detail = "You don't have permission to add clients to this case"
                });
            }

            // check if the client already exists
            var isAlreadyClient = caseDoc.Clients?.Any(c => c.UserId == request.UserID) ?? false;
            if (isAlreadyClient)
            {
                return StatusCode(StatusCodes.Status409Conflict, new FailureResponseModel
                {
                    Detail = "Client is already added to this case"
                });
            }

            // add client
            await _caseDocService.AddClientAsync(
                caseId: caseId,
                userId: request.UserID,
                actorUserId: user.Id
            );

            this.Logger.LogInformation(
                "Added client {ClientId} to case {CaseId} by user {UserId}",
                request.UserID, caseId, user.Id
            );

            return StatusCode(StatusCodes.Status201Created, new SuccessResponseModel());
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Failed to add client to case {CaseId}", caseId);
            return StatusCode(StatusCodes.Status500InternalServerError, new FailureResponseModel { Detail = "Failed to add client" });
        }
    }

    [HttpDelete("clients/{clientId:guid}")]
    [ProducesResponseType(typeof(SuccessResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SuccessResponseModel>> RemoveClientFromCase(
        Guid caseId,
        Guid clientId
    )
    {
        try
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            // grab the case with workers
            var caseDoc = await _caseDocService.GetDocumentAsync(caseId);
            if (caseDoc == null)
            {
                return StatusCode(StatusCodes.Status404NotFound, new FailureResponseModel { Detail = "Case not found" });
            }

            // only admins or existing workers can remove clients
            var isWorker = caseDoc.Workers?.Any(w => w.UserId == user!.Id) ?? false;
            if (!user!.IsAdministrator && !isWorker)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new FailureResponseModel
                {
                    Detail = "You don't have permission to remove clients from this case"
                });
            }

            // remove the client
            await _caseDocService.RemoveClientAsync(
                caseId: caseId,
                userId: clientId,
                actorUserId: user.Id
            );

            this.Logger.LogInformation(
                "Removed client {ClientId} from case {CaseId} by user {UserId}",
                clientId, caseId, user.Id
            );

            return StatusCode(StatusCodes.Status200OK, new SuccessResponseModel());
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Failed to remove client from case {CaseId}", caseId);
            return StatusCode(StatusCodes.Status500InternalServerError, new FailureResponseModel { Detail = "Failed to remove client" });
        }
    }
    #endregion

    #region ========================= WORKERS =========================
    [HttpPost("workers")]
    [ProducesResponseType(typeof(SuccessResponseModel), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SuccessResponseModel>> AddWorkerToCase(
        Guid caseId,
        [FromBody] AddPersonRequestModel request
    )
    {
        try
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            // grab the case with workers
            var caseDoc = await _caseDocService.GetDocumentAsync(caseId);
            if (caseDoc == null)
            {
                return StatusCode(StatusCodes.Status404NotFound, new FailureResponseModel { Detail = "Case not found" });
            }

            // only admins or existing workers can add workers
            var isWorker = caseDoc.Workers?.Any(w => w.UserId == user!.Id) ?? false;
            if (!user!.IsAdministrator && !isWorker)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new FailureResponseModel
                {
                    Detail = "You don't have permission to add workers to this case"
                });
            }

            // check if worker already exists
            var isAlreadyWorker = caseDoc.Workers?.Any(w => w.UserId == request.UserID) ?? false;
            if (isAlreadyWorker)
            {
                return StatusCode(StatusCodes.Status409Conflict, new FailureResponseModel
                {
                    Detail = "Worker is already added to this case"
                });
            }

            // add worker
            await _caseDocService.AddWorkerAsync(
                caseId: caseId,
                userId: request.UserID,
                actorUserId: user.Id
            );

            this.Logger.LogInformation(
                "Added worker {WorkerId} to case {CaseId} by user {UserId}",
                request.UserID, caseId, user.Id
            );

            return StatusCode(StatusCodes.Status201Created, new SuccessResponseModel());
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Failed to add worker to case {CaseId}", caseId);
            return StatusCode(StatusCodes.Status500InternalServerError, new FailureResponseModel { Detail = "Failed to add worker" });
        }
    }

    [HttpDelete("workers/{workerId:guid}")]
    [ProducesResponseType(typeof(SuccessResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SuccessResponseModel>> RemoveWorkerFromCase(
        Guid caseId,
        Guid workerId
    )
    {
        try
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            // get case with workers
            var caseDoc = await _caseDocService.GetDocumentAsync(caseId);
            if (caseDoc == null)
            {
                return StatusCode(StatusCodes.Status404NotFound, new FailureResponseModel { Detail = "Case not found" });
            }

            // only admins or existing workers can remove workers
            var isWorker = caseDoc.Workers?.Any(w => w.UserId == user!.Id) ?? false;
            if (!user!.IsAdministrator && !isWorker)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new FailureResponseModel
                {
                    Detail = "You don't have permission to remove workers from this case"
                });
            }

            // remove worker
            await _caseDocService.RemoveWorkerAsync(
                caseId: caseId,
                userId: workerId,
                actorUserId: user.Id
            );

            this.Logger.LogInformation(
                "Removed worker {WorkerId} from case {CaseId} by user {UserId}",
                workerId, caseId, user.Id
            );

            return StatusCode(StatusCodes.Status200OK, new SuccessResponseModel());
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Failed to remove worker from case {CaseId}", caseId);
            return StatusCode(StatusCodes.Status500InternalServerError, new FailureResponseModel { Detail = "Failed to remove worker" });
        }
    }
    #endregion
}
