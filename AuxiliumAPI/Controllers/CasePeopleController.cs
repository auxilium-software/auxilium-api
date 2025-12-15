using AuxiliumAPI.Common.ControllerBases;
using AuxiliumAPI.Common.DataStructures.CouchDB;
using AuxiliumAPI.Common.DataStructures.MariaDB;
using AuxiliumAPI.Common.Enumerators;
using AuxiliumAPI.Common.Services.Interfaces;
using AuxiliumAPI.Common.Utilities;
using AuxiliumAPI.Models;
using AuxiliumAPI.Models.Case;
using AuxiliumAPI.Models.UserLogin;
using AuxiliumAPI.Models.UserRefresh;
using AuxiliumAPI.Models.UserRegistration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AuxiliumAPI.Controllers
{
    [ApiController]
    [Route("/api/v3/cases/{caseId}")]
    [Tags("Cases")]
    public class CasePeopleController : LoggedInControllerBase
    {
        private readonly ICaseDocumentService _caseDocService;
        private readonly ICouchDbService _couchDb;
        private readonly ILogger<CasePeopleController> _logger;

        public CasePeopleController(
            ICaseDocumentService caseDocService,
            ICouchDbService couchDb,
            ILogger<CasePeopleController> logger,
            IMariaDbService mariaDb)
            : base(mariaDb, logger)
        {
            _caseDocService = caseDocService;
            _couchDb = couchDb;
            _logger = logger;
        }

        #region Clients
        [HttpPost("clients")]
        [ProducesResponseType(typeof(CaseResponseModel), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
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

                // check the case id
                if (!Guid.TryParse(caseId, out _))
                {
                    return BadRequest(new FailureResponseModel { Detail = "Invalid case ID" });
                }

                // grab the case doc from couchdb
                var caseDoc = await _caseDocService.GetDocumentAsync(Guid.Parse(caseId));
                if (caseDoc == null)
                {
                    return NotFound(new FailureResponseModel { Detail = "Case not found" });
                }

                // check permissions - only admins or existing workers can add new clients
                if (!user!.is_admin && !caseDoc.Workers.Contains(user.id))
                {
                    return StatusCode(403, new FailureResponseModel
                    {
                        Detail = "You don't have permission to add clients to this case"
                    });
                }

                // check if the user is already added to the case
                if (caseDoc.Clients.Contains(request.UserID))
                {
                    return Conflict(new FailureResponseModel
                    {
                        Detail = "Client is already added to this case"
                    });
                }

                // add the client to the case
                await _caseDocService.AddClientAsync(Guid.Parse(caseId), request.UserID);

                // return
                return StatusCode(201, new SuccessResponseModel());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add client to case {CaseId}", caseId);
                return StatusCode(500, new FailureResponseModel { Detail = "Failed to add client" });
            }
        }

        [HttpDelete("clients/{clientId}")]
        [ProducesResponseType(typeof(SuccessResponseModel), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<SuccessResponseModel>> RemoveClientFromCase(
            string caseId,
            string clientId
            )
        {
            try
            {
                var (user, error) = await GetCurrentUserAsync();
                if (error != null) return error;

                // check the case and client id
                if (!Guid.TryParse(caseId, out _))
                {
                    return BadRequest(new FailureResponseModel { Detail = "Invalid case ID" });
                }
                if (!Guid.TryParse(clientId, out _))
                {
                    return BadRequest(new FailureResponseModel { Detail = "Invalid client ID" });
                }

                // grab the case doc from couchdb
                var caseDoc = await _caseDocService.GetDocumentAsync(Guid.Parse(caseId));
                if (caseDoc == null)
                {
                    return NotFound(new FailureResponseModel { Detail = "Case not found" });
                }

                // check permissions - only admins or existing workers can remove clients
                if (!user!.is_admin && !caseDoc.Workers.Contains(user.id))
                {
                    return StatusCode(403, new FailureResponseModel
                    {
                        Detail = "You don't have permission to remove clients from this case"
                    });
                }

                // remove the client from the case
                await _caseDocService.RemoveClientAsync(Guid.Parse(caseId), Guid.Parse(clientId));

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

        #region Workers
        [HttpPost("workers")]
        [ProducesResponseType(typeof(CaseResponseModel), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
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

                // check the case id
                if (!Guid.TryParse(caseId, out _))
                {
                    return BadRequest(new FailureResponseModel { Detail = "Invalid case ID" });
                }

                // grab the case doc from couchdb
                var caseDoc = await _caseDocService.GetDocumentAsync(Guid.Parse(caseId));
                if (caseDoc == null)
                {
                    return NotFound(new FailureResponseModel { Detail = "Case not found" });
                }

                // check permissions - only admins or existing workers can add new workers
                if (!user!.is_admin && !caseDoc.Workers.Contains(user.id))
                {
                    return StatusCode(403, new FailureResponseModel
                    {
                        Detail = "You don't have permission to add workers to this case"
                    });
                }

                // check if the worker is already added to the case
                if (caseDoc.Workers.Contains(request.UserID))
                {
                    return Conflict(new FailureResponseModel
                    {
                        Detail = "Worker is already added to this case"
                    });
                }

                // add the worker to the case
                await _caseDocService.AddWorkerAsync(Guid.Parse(caseId), request.UserID);

                // return
                return StatusCode(201, new SuccessResponseModel());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add worker to case {CaseId}", caseId);
                return StatusCode(500, new FailureResponseModel { Detail = "Failed to add worker" });
            }
        }

        [HttpDelete("workers/{workerId}")]
        [ProducesResponseType(typeof(SuccessResponseModel), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<SuccessResponseModel>> RemoveWorkerFromCase(
            string caseId,
            string workerId
            )
        {
            try
            {
                var (user, error) = await GetCurrentUserAsync();
                if (error != null) return error;

                // check the case and worker id
                if (!Guid.TryParse(caseId, out _))
                {
                    return BadRequest(new FailureResponseModel { Detail = "Invalid case ID" });
                }
                if (!Guid.TryParse(workerId, out _))
                {
                    return BadRequest(new FailureResponseModel { Detail = "Invalid worker ID" });
                }

                // grab the case doc from couchdb
                var caseDoc = await _caseDocService.GetDocumentAsync(Guid.Parse(caseId));
                if (caseDoc == null)
                {
                    return NotFound(new FailureResponseModel { Detail = "Case not found" });
                }

                // check permissions - only admins or existing workers can remove workers
                if (!user!.is_admin && !caseDoc.Workers.Contains(user.id))
                {
                    return StatusCode(403, new FailureResponseModel
                    {
                        Detail = "You don't have permission to remove workers from this case"
                    });
                }

                // remove the worker from the case
                await _caseDocService.RemoveWorkerAsync(Guid.Parse(caseId), Guid.Parse(workerId));

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
}
