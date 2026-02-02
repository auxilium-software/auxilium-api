using AuxiliumSoftware.AuxiliumServices.API.Common.ControllerBases;
using AuxiliumSoftware.AuxiliumServices.API.Models;
using AuxiliumSoftware.AuxiliumServices.API.Models.CaseMessage;
using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework;
using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework.EntityModels;
using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework.Enumerators;
using AuxiliumSoftware.AuxiliumServices.Common.Services;
using Microsoft.AspNetCore.Mvc;

namespace AuxiliumSoftware.AuxiliumServices.API.Controllers;

[ApiController]
[Route("api/v3/cases/{caseId}/messages")]
[Tags("Cases", "Messages")]
public class CaseMessagesController : LoggedInControllerBase
{
    private readonly IMessageDocumentService _messageService;
    private readonly ICaseDocumentService _caseDocService;

    public CaseMessagesController(
        IConfiguration configuration,
        AuxiliumDbContext db,
        ILogger<CaseMessagesController> logger,
        ITotpService totpService,

        IMessageDocumentService messageService,
        ICaseDocumentService caseDocService
        )
        : base(configuration, db, logger, totpService)
    {
        _messageService = messageService;
        _caseDocService = caseDocService;
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

            // Check case access
            if (!await _caseDocService.CheckUserAccessAsync(caseGuid, user!))
            {
                return StatusCode(403, new FailureResponseModel
                {
                    Detail = "You don't have permission to add messages to this case"
                });
            }

            // Create the message
            var messageDoc = await _messageService.CreateMessageAsync(
                caseId: caseGuid,
                subject: request.Subject,
                content: request.Content,
                senderId: user!.Id,
                isUrgent: request.IsUrgent
            );

            this.Logger.LogInformation(
                "Message {MessageId} created in case {CaseId} by user {UserId}",
                messageDoc.Id, caseId, user.Id
            );

            // Get read-by details (should be empty for new message)
            var readByDetails = await _messageService.GetReadByDetailsAsync(messageDoc.Id);

            return StatusCode(201, new MessageResponseModel
            {
                Id = messageDoc.Id,
                CreatedBy = messageDoc.CreatedBy,
                CreatedAt = messageDoc.CreatedAt,
                Subject = messageDoc.Subject,
                Content = messageDoc.Content,
                SenderId = messageDoc.SenderId,
                IsUrgent = messageDoc.IsUrgent,
                ReadBy = readByDetails
            });
        }
        catch (KeyNotFoundException ex)
        {
            this.Logger.LogWarning(ex, "Case not found: {CaseId}", caseId);
            return NotFound(new FailureResponseModel { Detail = "Case not found" });
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Failed to create message in case {CaseId}", caseId);
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

            var response = new List<MessageResponseModel>();

            foreach (var msg in messages)
            {
                var readByDetails = await _messageService.GetReadByDetailsAsync(msg.Id);

                response.Add(new MessageResponseModel
                {
                    Id = msg.Id,
                    CreatedAt = msg.CreatedAt,
                    CreatedBy = msg.CreatedBy,
                    Subject = msg.Subject,
                    Content = msg.Content,
                    SenderId = msg.SenderId,
                    IsUrgent = msg.IsUrgent,
                    ReadBy = readByDetails
                });
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Failed to get messages for case {CaseId}", caseId);
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

            // Verify the message belongs to this case
            if (messageDoc.CaseId != caseGuid)
            {
                return NotFound(new FailureResponseModel { Detail = "Message not found in this case" });
            }

            // Mark the message as read
            await _messageService.MarkAsReadAsync(messageId, user!.Id);

            // Get read-by details
            var readByDetails = await _messageService.GetReadByDetailsAsync(messageId);

            return Ok(new MessageResponseModel
            {
                Id = messageDoc.Id,
                CreatedAt = messageDoc.CreatedAt,
                CreatedBy = messageDoc.CreatedBy,
                Subject = messageDoc.Subject,
                Content = messageDoc.Content,
                SenderId = messageDoc.SenderId,
                IsUrgent = messageDoc.IsUrgent,
                ReadBy = readByDetails
            });
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Failed to get message {MessageId}", messageId);
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

            // Check if the user is worker or admin
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

            // Verify the message actually exists and belongs to case
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

            this.Logger.LogInformation(
                "Message {MessageId} deleted from case {CaseId} by user {UserId}",
                messageId, caseId, user.Id
            );

            return Ok(new SuccessResponseModel());
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Failed to delete message {MessageId}", messageId);
            return StatusCode(500, new FailureResponseModel
            {
                Detail = "Failed to delete message"
            });
        }
    }
}
