using AuxiliumAPI.Common.ControllerBases;
using AuxiliumAPI.Common.EF;
using AuxiliumAPI.Common.Services.Interfaces;
using AuxiliumAPI.Models;
using AuxiliumAPI.Models.CaseMessage;
using Microsoft.AspNetCore.Mvc;

namespace AuxiliumAPI.Controllers;

[ApiController]
[Route("api/v3/cases/{caseId}/messages")]
[Tags("Cases", "Messages")]
public class CaseMessagesController : LoggedInControllerBase
{
    private readonly IMessageDocumentService _messageService;
    private readonly ICaseDocumentService _caseDocService;
    private readonly ILogger<CaseMessagesController> _logger;

    public CaseMessagesController(
        IMessageDocumentService messageService,
        ICaseDocumentService caseDocService,
        AuxiliumDbContext db,
        ILogger<CaseMessagesController> logger)
        : base(db, logger)
    {
        _messageService = messageService;
        _caseDocService = caseDocService;
        _logger = logger;
    }

    [HttpPost("")]
    [ProducesResponseType(typeof(MessageResponseModel), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<MessageResponseModel>> CreateMessage(
        string caseId,
        [FromBody] MessageCreationRequestModel request)
    {
        try
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            if (!Guid.TryParse(caseId, out var caseGuid))
            {
                return BadRequest(new FailureResponseModel { Detail = "Invalid case ID" });
            }

            // check case access
            if (!await _caseDocService.CheckUserAccessAsync(caseGuid, user!))
            {
                return StatusCode(403, new FailureResponseModel
                {
                    Detail = "You don't have permission to add messages to this case"
                });
            }

            // create the message entitymessage
            var messageDoc = await _messageService.CreateMessageAsync(
                caseId: caseGuid,
                subject: request.Subject,
                content: request.Content,
                senderId: user.Id,
                isUrgent: request.IsUrgent
            );

            _logger.LogInformation(
                "Message {MessageId} created in case {CaseId} by user {UserId}",
                messageDoc.Id, caseId, user.Id
            );

            return StatusCode(201, new MessageResponseModel
            {
                Id = messageDoc.Id,
                CreatedBy = messageDoc.CreatedBy,
                CreatedAt = messageDoc.CreatedAt,
                Subject = messageDoc.Subject,
                Content = messageDoc.Content,
                SenderId = messageDoc.SenderId,
                IsUrgent = messageDoc.IsUrgent,
                ReadBy = messageDoc.ReadBy ?? "{}"
            });
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Case not found: {CaseId}", caseId);
            return NotFound(new FailureResponseModel { Detail = "Case not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create message in case {CaseId}", caseId);
            return StatusCode(500, new FailureResponseModel
            {
                Detail = "Failed to create message"
            });
        }
    }

    [HttpGet("")]
    [ProducesResponseType(typeof(List<MessageResponseModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<MessageResponseModel>>> GetMessages(string caseId)
    {
        try
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            if (!Guid.TryParse(caseId, out var caseGuid))
            {
                return BadRequest(new FailureResponseModel { Detail = "Invalid case ID" });
            }

            if (!await _caseDocService.CheckUserAccessAsync(caseGuid, user!))
            {
                return StatusCode(403, new FailureResponseModel
                {
                    Detail = "You don't have permission to view messages in this case"
                });
            }

            var messages = await _messageService.GetMessagesForCaseAsync(caseGuid);

            var response = messages.Select(m => new MessageResponseModel
            {
                Id = m.Id,
                CreatedAt = m.CreatedAt,
                CreatedBy = m.CreatedBy,
                Subject = m.Subject,
                Content = m.Content,
                SenderId = m.SenderId,
                IsUrgent = m.IsUrgent,
                ReadBy = m.ReadBy ?? "{}",
            }).ToList();

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get messages for case {CaseId}", caseId);
            return StatusCode(500, new FailureResponseModel
            {
                Detail = "Failed to retrieve messages"
            });
        }
    }

    [HttpGet("{messageId:guid}")]
    [ProducesResponseType(typeof(MessageResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<MessageResponseModel>> GetMessage(
        string caseId,
        Guid messageId)
    {
        try
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            if (!Guid.TryParse(caseId, out var caseGuid))
            {
                return BadRequest(new FailureResponseModel { Detail = "Invalid case ID" });
            }

            if (!await _caseDocService.CheckUserAccessAsync(caseGuid, user!))
            {
                return StatusCode(403, new FailureResponseModel
                {
                    Detail = "You don't have permission to view messages in this case"
                });
            }

            var messageDoc = await _messageService.GetMessageAsync(messageId);
            if (messageDoc == null)
            {
                return NotFound(new FailureResponseModel { Detail = "Message not found" });
            }

            // verify the message belongs to this case
            if (messageDoc.CaseId != caseGuid)
            {
                return NotFound(new FailureResponseModel { Detail = "Message not found in this case" });
            }

            // mark the message as read
            await _messageService.MarkAsReadAsync(messageId, user.Id);

            return Ok(new MessageResponseModel
            {
                Id = messageDoc.Id,
                CreatedAt = messageDoc.CreatedAt,
                CreatedBy = messageDoc.CreatedBy,
                Subject = messageDoc.Subject,
                Content = messageDoc.Content,
                SenderId = messageDoc.SenderId,
                IsUrgent = messageDoc.IsUrgent,
                ReadBy = messageDoc.ReadBy ?? "{}",
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get message {MessageId}", messageId);
            return StatusCode(500, new FailureResponseModel
            {
                Detail = "Failed to retrieve message"
            });
        }
    }

    [HttpDelete("{messageId:guid}")]
    [ProducesResponseType(typeof(SuccessResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SuccessResponseModel>> DeleteMessage(
        string caseId,
        Guid messageId)
    {
        try
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            if (!Guid.TryParse(caseId, out var caseGuid))
            {
                return BadRequest(new FailureResponseModel { Detail = "Invalid case ID" });
            }

            // check if the user is worker or admin
            var caseDoc = await _caseDocService.GetDocumentAsync(caseGuid);
            if (caseDoc == null)
            {
                return NotFound(new FailureResponseModel { Detail = "Case not found" });
            }

            var isWorker = caseDoc.Workers?.Any(w => w.UserId == user!.Id) ?? false;
            if (!user!.IsAdmin && !isWorker)
            {
                return StatusCode(403, new FailureResponseModel
                {
                    Detail = "Only case workers and admins can delete messages"
                });
            }

            // verify the message actually exists and belongs to case
            var message = await _messageService.GetMessageAsync(messageId);
            if (message == null)
            {
                return NotFound(new FailureResponseModel { Detail = "Message not found" });
            }

            if (message.CaseId != caseGuid)
            {
                return NotFound(new FailureResponseModel { Detail = "Message not found in this case" });
            }

            await _messageService.DeleteMessageAsync(messageId);

            _logger.LogInformation(
                "Message {MessageId} deleted from case {CaseId} by user {UserId}",
                messageId, caseId, user.Id
            );

            return Ok(new SuccessResponseModel());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete message {MessageId}", messageId);
            return StatusCode(500, new FailureResponseModel
            {
                Detail = "Failed to delete message"
            });
        }
    }
}
