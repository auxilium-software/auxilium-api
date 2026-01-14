using AuxiliumSoftware.AuxiliumServices.Common.EF;
using AuxiliumSoftware.AuxiliumServices.Common.Enumerators;
using AuxiliumSoftware.AuxiliumServices.Common.Services.Interfaces;
using AuxiliumAPI.Models;
using Microsoft.AspNetCore.Mvc;
using AuxiliumSoftware.AuxiliumServices.API.Models;
using AuxiliumSoftware.AuxiliumServices.API.Common.ControllerBases;
using AuxiliumSoftware.AuxiliumServices.API.Models.Case;

namespace AuxiliumSoftware.AuxiliumServices.API.Controllers;

[ApiController]
[Route("/api/v3/cases/{caseId}/todos")]
[Tags("Cases")]
public class CaseTodoController : LoggedInControllerBase
{
    private readonly ILogger<CaseTodoController> _logger;

    private readonly ICaseDocumentService _caseDocService;

    public CaseTodoController(
        ILogger<CaseTodoController> logger,

        ICaseDocumentService caseDocService,
        AuxiliumDbContext db
        )
        : base(db, logger)
    {
        _logger = logger;

        _caseDocService = caseDocService;
    }

    [HttpPost("")]
    [ProducesResponseType(typeof(TodoResponseModel), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<TodoResponseModel>> CreateTodo(
        string caseId,
        [FromBody] TodoCreationRequestModel request)
    {
        try
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            // make sure the given case id is a valid uuid
            if (!Guid.TryParse(caseId, out var caseGuid))
            {
                return BadRequest(new FailureResponseModel { Detail = "Invalid case ID" });
            }

            // check the user's access to the case
            if (!await _caseDocService.CheckUserAccessAsync(caseGuid, user!))
            {
                return StatusCode(403, new FailureResponseModel
                {
                    Detail = "You don't have permission to add todos to this case"
                });
            }

            // create the todo
            var todo = await _caseDocService.CreateTodoAsync(
                caseId: caseGuid,
                summary: request.Summary,
                description: request.Description,
                priority: request.Priority,
                createdBy: user!.Id,
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
                    CaseId = caseGuid,
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

    [HttpPatch("{todoId:guid}")]
    [ProducesResponseType(typeof(SuccessResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SuccessResponseModel>> UpdateTodo(
        string caseId,
        Guid todoId,
        [FromBody] TodoUpdateRequestModel request)
    {
        try
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            // make sure the given case id is a valid uuid
            if (!Guid.TryParse(caseId, out var caseGuid))
            {
                return BadRequest(new FailureResponseModel { Detail = "Invalid case ID" });
            }

            // check the user's access to the case
            if (!await _caseDocService.CheckUserAccessAsync(caseGuid, user!))
            {
                return StatusCode(403, new FailureResponseModel
                {
                    Detail = "You don't have permission to update todos for this case"
                });
            }

            // update the todo
            await _caseDocService.UpdateTodoAsync(
                caseGuid,
                todoId,
                request.Summary,
                request.Description,
                request.Priority,
                request.DueDate,
                request.AssignedTo,
                request.Reminder
            );

            // update status is set
            if (request.Status.HasValue)
            {
                await _caseDocService.UpdateTodoStatusAsync(
                    caseGuid,
                    todoId,
                    request.Status.Value,
                    request.Status == TodoStatusEnum.Completed ? user!.Id : null,
                    request.CompletionNote
                );
            }

            // return
            return Ok(new SuccessResponseModel());
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new FailureResponseModel { Detail = "Todo not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update todo {TodoId}", todoId);
            return StatusCode(500, new FailureResponseModel { Detail = "Failed to update todo" });
        }
    }

    [HttpDelete("{todoId:guid}")]
    [ProducesResponseType(typeof(SuccessResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SuccessResponseModel>> DeleteTodo(
        string caseId,
        Guid todoId)
    {
        try
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            // make sure the given case id is a valid uuid
            if (!Guid.TryParse(caseId, out var caseGuid))
            {
                return BadRequest(new FailureResponseModel { Detail = "Invalid case ID" });
            }

            // check the user's access to the case
            var caseDoc = await _caseDocService.GetDocumentAsync(caseGuid);
            if (caseDoc == null)
            {
                return NotFound(new FailureResponseModel { Detail = "Case not found" });
            }

            // only admins and case workers can delete todos
            var isWorker = caseDoc.Workers?.Any(w => w.UserId == user!.Id) ?? false;
            if (!user!.IsAdmin && !isWorker)
            {
                return StatusCode(403, new FailureResponseModel
                {
                    Detail = "Only case workers and admins can delete todos"
                });
            }

            // delete the todo
            await _caseDocService.DeleteTodoAsync(caseGuid, todoId);

            // return
            return Ok(new SuccessResponseModel());
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new FailureResponseModel { Detail = "Todo not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete todo {TodoId}", todoId);
            return StatusCode(500, new FailureResponseModel { Detail = "Failed to delete todo" });
        }
    }
}
