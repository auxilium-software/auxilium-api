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
[Route("/api/v3/cases/{caseId:guid}/todos")]
[Tags("Cases")]
public class SingleCaseTodoController : LoggedInControllerBase
{
    private readonly ICaseDocumentService _caseDocService;

    public SingleCaseTodoController(
        ISystemSettingsService systemSettingsService,
        IConfiguration configuration,
        AuxiliumDbContext db,
        IWebApplicationFirewallService waf,
        ILogger<SingleCaseTodoController> logger,
        ITotpService totpService,

        ICaseDocumentService caseDocService
        )
        : base(systemSettingsService, configuration, db, waf, logger, totpService)
    {
        _caseDocService = caseDocService;
    }

    [HttpPost("")]
    [ProducesResponseType(typeof(TodoResponseModel), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<TodoResponseModel>> CreateTodo(
        Guid caseId,
        [FromBody] TodoCreationRequestModel request
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
                    Detail = "You don't have permission to add todos to this case"
                });
            }

            // create the todo
            var todo = await _caseDocService.CreateTodoAsync(
                caseId: caseId,
                summary: request.Summary,
                description: request.Description,
                priority: request.Priority,
                createdBy: user!.Id,
                dueDate: request.DueDate,
                assignedTo: request.AssignedTo,
                reminder: request.Reminder
            );

            return StatusCode(StatusCodes.Status201Created, new TodoResponseModel
            {
                Id = todo.Id,
                CaseId = caseId,
                Summary = todo.Summary,
                Description = todo.Description,
                Status = todo.Status,
                Priority = todo.Priority,
                CreatedAt = todo.CreatedAtUtc,
                CreatedBy = todo.CreatedBy,
                DueDate = todo.DueDate,
                CompletedAt = todo.CompletedAtUtc,
                CompletedBy = todo.CompletedBy,
                AssignedTo = todo.AssignedTo,
                CompletionNote = todo.CompletionNote
            });
        }
        catch (KeyNotFoundException ex)
        {
            this.Logger.LogWarning(ex, "Case not found: {CaseId}", caseId);
            return StatusCode(StatusCodes.Status404NotFound, new FailureResponseModel { Detail = "Case not found" });
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Failed to create todo in case {CaseId}", caseId);
            return StatusCode(StatusCodes.Status500InternalServerError, new FailureResponseModel { Detail = "Failed to create todo" });
        }
    }

    [HttpPatch("{todoId:guid}")]
    [ProducesResponseType(typeof(SuccessResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SuccessResponseModel>> UpdateTodo(
        Guid caseId,
        Guid todoId,
        [FromBody] TodoUpdateRequestModel request
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
                    Detail = "You don't have permission to update todos for this case"
                });
            }

            // update the todo
            await _caseDocService.UpdateTodoAsync(
                caseId,
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
                    caseId,
                    todoId,
                    request.Status.Value,
                    request.Status == TodoStatusEnum.Completed ? user!.Id : null,
                    request.CompletionNote
                );
            }

            return StatusCode(StatusCodes.Status200OK, new SuccessResponseModel());
        }
        catch (KeyNotFoundException)
        {
            return StatusCode(StatusCodes.Status404NotFound, new FailureResponseModel { Detail = "Todo not found" });
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Failed to update todo {TodoId}", todoId);
            return StatusCode(StatusCodes.Status500InternalServerError, new FailureResponseModel { Detail = "Failed to update todo" });
        }
    }

    [HttpDelete("{todoId:guid}")]
    [ProducesResponseType(typeof(SuccessResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SuccessResponseModel>> DeleteTodo(
        Guid caseId,
        Guid todoId
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

            // only admins and case workers can delete todos
            var isWorker = caseDoc.Workers?.Any(w => w.UserId == user!.Id) ?? false;
            if (!user!.IsAdministrator && !isWorker)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new FailureResponseModel
                {
                    Detail = "Only case workers and admins can delete todos"
                });
            }

            // delete the todo
            await _caseDocService.DeleteTodoAsync(caseId, todoId);

            return StatusCode(StatusCodes.Status200OK, new SuccessResponseModel());
        }
        catch (KeyNotFoundException)
        {
            return StatusCode(StatusCodes.Status404NotFound, new FailureResponseModel { Detail = "Todo not found" });
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Failed to delete todo {TodoId}", todoId);
            return StatusCode(StatusCodes.Status500InternalServerError, new FailureResponseModel { Detail = "Failed to delete todo" });
        }
    }
}
