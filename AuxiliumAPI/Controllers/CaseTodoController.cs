using AuxiliumAPI.Common.ControllerBases;
using AuxiliumAPI.Common.DataStructures.CouchDB;
using AuxiliumAPI.Common.DataStructures.CouchDB.SubStructures;
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
using Microsoft.Win32;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AuxiliumAPI.Controllers
{
    [ApiController]
    [Route("/api/v3/cases/{case_id}/todos")]
    [Tags("Cases")]
    public class CaseTodoController : LoggedInControllerBase
    {

        private readonly ICaseDocumentService _caseDocService;
        private readonly ICouchDbService _couchDb;
        private readonly ILogger<CaseTodoController> _logger;

        public CaseTodoController(
            ICaseDocumentService caseDocService,
            ICouchDbService couchDb,
            ILogger<CaseTodoController> logger,
            IMariaDbService mariaDb)
            : base(mariaDb, logger)
        {
            _caseDocService = caseDocService;
            _couchDb = couchDb;
            _logger = logger;
        }


        [HttpPost("")]
        [ProducesResponseType(typeof(TodoResponseModel), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<TodoResponseModel>> CreateTodo(
            string caseId,
            [FromBody] TodoCreationRequestModel request
            )
        {
            try
            {
                var (user, error) = await GetCurrentUserAsync();
                if (error != null) return error;

                // make sure the given case id is a valid uuid
                if (!Guid.TryParse(caseId, out _))
                {
                    return BadRequest(new FailureResponseModel { Detail = "Invalid case ID" });
                }

                // check the user's access to the case
                if (!await _caseDocService.CheckUserAccessAsync(Guid.Parse(caseId), user!))
                {
                    return StatusCode(403, new FailureResponseModel
                    {
                        Detail = "You don't have permission to add todos to this case"
                    });
                }

                // create the todo sub structure
                CaseTodoSubStructure todo = await _caseDocService.CreateTodoAsync(
                    caseId: Guid.Parse(caseId),
                    summary: request.Summary,
                    description: request.Description,
                    priority: request.Priority,
                    createdBy: user.id,
                    dueDate: request.DueDate,
                    assignedTo: request.AssignedTo,
                    reminder: request.Reminder
                );

                // return
                return CreatedAtAction(
                    nameof(CreateTodo),
                    new TodoResponseModel
                    {
                        Id = todo.Id,
                        CaseId = Guid.Parse(caseId),
                        Summary = todo.Summary,
                        Description = todo.Description,
                        Status = todo.Status,
                        Priority = todo.Priority,
                        CreatedAt = todo.CreatedAt,
                        CreatedBy = todo.CreatedBy,
                        DueDate = todo.DueDate,
                        CompletedAt = todo.CompletedAt,
                        CompletedBy = todo.CompletedBy,
                        AssignedTo = todo.AssignedTo,
                        CompletionNote = todo.CompletionNote
                    }
                );
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Case not found: {CaseId}", caseId);
                return NotFound(new FailureResponseModel { Detail = "Case not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create todo in case {CaseId}", caseId);
                return StatusCode(500, new FailureResponseModel { Detail = "Failed to create todo" });
            }
        }
    }
}
