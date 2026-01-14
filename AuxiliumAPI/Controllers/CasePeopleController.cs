using AuxiliumAPI.Common.ControllerBases;
using AuxiliumSoftware.AuxiliumServices.Common.EF;
using AuxiliumSoftware.AuxiliumServices.Common.Services.Interfaces;
using AuxiliumAPI.Models;
using AuxiliumAPI.Models.Case;
using Microsoft.AspNetCore.Mvc;

namespace AuxiliumAPI.Controllers;

[ApiController]
[Route("/api/v3/cases/{caseId}")]
[Tags("Cases")]
public class CasePeopleController : LoggedInControllerBase
{
    private readonly ILogger<CasePeopleController> _logger;

    private readonly ICaseDocumentService _caseDocService;

    public CasePeopleController(
        ILogger<CasePeopleController> logger,

        ICaseDocumentService caseDocService,
        AuxiliumDbContext db
        )
        : base(db, logger)
    {
        _logger = logger;

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
        string caseId,
        [FromBody] AddPersonRequestModel request)
    {
        try
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            if (!Guid.TryParse(caseId, out var caseGuid))
            {
                return BadRequest(new FailureResponseModel { Detail = "Invalid case ID" });
            }

            // grab the case with workers
            var caseDoc = await _caseDocService.GetDocumentAsync(caseGuid);
            if (caseDoc == null)
            {
                return NotFound(new FailureResponseModel { Detail = "Case not found" });
            }

            // only admins or existing workers can add clients
            var isWorker = caseDoc.Workers?.Any(w => w.UserId == user!.Id) ?? false;
            if (!user!.IsAdmin && !isWorker)
            {
                return StatusCode(403, new FailureResponseModel
                {
                    Detail = "You don't have permission to add clients to this case"
                });
            }

            // check if the client already exists
            var isAlreadyClient = caseDoc.Clients?.Any(c => c.UserId == request.UserID) ?? false;
            if (isAlreadyClient)
            {
                return Conflict(new FailureResponseModel
                {
                    Detail = "Client is already added to this case"
                });
            }

            // add client
            await _caseDocService.AddClientAsync(caseGuid, request.UserID);

            _logger.LogInformation(
                "Added client {ClientId} to case {CaseId} by user {UserId}",
                request.UserID, caseId, user.Id
            );

            return StatusCode(201, new SuccessResponseModel());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add client to case {CaseId}", caseId);
            return StatusCode(500, new FailureResponseModel { Detail = "Failed to add client" });
        }
    }

    [HttpDelete("clients/{clientId:guid}")]
    [ProducesResponseType(typeof(SuccessResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SuccessResponseModel>> RemoveClientFromCase(
        string caseId,
        Guid clientId)
    {
        try
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            if (!Guid.TryParse(caseId, out var caseGuid))
            {
                return BadRequest(new FailureResponseModel { Detail = "Invalid case ID" });
            }

            // grab the case with workers
            var caseDoc = await _caseDocService.GetDocumentAsync(caseGuid);
            if (caseDoc == null)
            {
                return NotFound(new FailureResponseModel { Detail = "Case not found" });
            }

            // only admins or existing workers can remove clients
            var isWorker = caseDoc.Workers?.Any(w => w.UserId == user!.Id) ?? false;
            if (!user!.IsAdmin && !isWorker)
            {
                return StatusCode(403, new FailureResponseModel
                {
                    Detail = "You don't have permission to remove clients from this case"
                });
            }

            // remove the client
            await _caseDocService.RemoveClientAsync(caseGuid, clientId);

            _logger.LogInformation(
                "Removed client {ClientId} from case {CaseId} by user {UserId}",
                clientId, caseId, user.Id
            );

            // return
            return Ok(new SuccessResponseModel());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove client from case {CaseId}", caseId);
            return StatusCode(500, new FailureResponseModel { Detail = "Failed to remove client" });
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
        string caseId,
        [FromBody] AddPersonRequestModel request)
    {
        try
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            if (!Guid.TryParse(caseId, out var caseGuid))
            {
                return BadRequest(new FailureResponseModel { Detail = "Invalid case ID" });
            }

            // grab the case with workers
            var caseDoc = await _caseDocService.GetDocumentAsync(caseGuid);
            if (caseDoc == null)
            {
                return NotFound(new FailureResponseModel { Detail = "Case not found" });
            }

            // only admins or existing workers can add workers
            var isWorker = caseDoc.Workers?.Any(w => w.UserId == user!.Id) ?? false;
            if (!user!.IsAdmin && !isWorker)
            {
                return StatusCode(403, new FailureResponseModel
                {
                    Detail = "You don't have permission to add workers to this case"
                });
            }

            // check if worker already exists
            var isAlreadyWorker = caseDoc.Workers?.Any(w => w.UserId == request.UserID) ?? false;
            if (isAlreadyWorker)
            {
                return Conflict(new FailureResponseModel
                {
                    Detail = "Worker is already added to this case"
                });
            }

            // add worker
            await _caseDocService.AddWorkerAsync(caseGuid, request.UserID);

            _logger.LogInformation(
                "Added worker {WorkerId} to case {CaseId} by user {UserId}",
                request.UserID, caseId, user.Id
            );

            // return
            return StatusCode(201, new SuccessResponseModel());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add worker to case {CaseId}", caseId);
            return StatusCode(500, new FailureResponseModel { Detail = "Failed to add worker" });
        }
    }

    [HttpDelete("workers/{workerId:guid}")]
    [ProducesResponseType(typeof(SuccessResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SuccessResponseModel>> RemoveWorkerFromCase(
        string caseId,
        Guid workerId)
    {
        try
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            if (!Guid.TryParse(caseId, out var caseGuid))
            {
                return BadRequest(new FailureResponseModel { Detail = "Invalid case ID" });
            }

            // get case with workers
            var caseDoc = await _caseDocService.GetDocumentAsync(caseGuid);
            if (caseDoc == null)
            {
                return NotFound(new FailureResponseModel { Detail = "Case not found" });
            }

            // only admins or existing workers can remove workers
            var isWorker = caseDoc.Workers?.Any(w => w.UserId == user!.Id) ?? false;
            if (!user!.IsAdmin && !isWorker)
            {
                return StatusCode(403, new FailureResponseModel
                {
                    Detail = "You don't have permission to remove workers from this case"
                });
            }

            // remove worker
            await _caseDocService.RemoveWorkerAsync(caseGuid, workerId);

            _logger.LogInformation(
                "Removed worker {WorkerId} from case {CaseId} by user {UserId}",
                workerId, caseId, user.Id
            );

            // return
            return Ok(new SuccessResponseModel());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove worker from case {CaseId}", caseId);
            return StatusCode(500, new FailureResponseModel { Detail = "Failed to remove worker" });
        }
    }
    #endregion
}
