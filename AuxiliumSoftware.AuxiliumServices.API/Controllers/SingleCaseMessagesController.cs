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
[Route("api/v3/cases/{caseId:guid}/messages")]
[Tags("Cases", "Messages")]
public class SingleCaseMessagesController : LoggedInControllerBase
{
    private readonly IMessageDocumentService _messageService;
    private readonly ICaseDocumentService _caseDocService;

    public SingleCaseMessagesController(
        ISystemSettingsService systemSettingsService,
        IConfiguration configuration,
        AuxiliumDbContext db,
        IWebApplicationFirewallService waf,
        ILogger<SingleCaseMessagesController> logger,
        ITotpService totpService,

        IMessageDocumentService messageService,
        ICaseDocumentService caseDocService
        )
        : base(systemSettingsService, configuration, db, waf, logger, totpService)
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
        Guid caseId,
        [FromBody] MessageCreationRequestModel request)
    {
        try
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            // check case access
            if (!await _caseDocService.CheckUserAccessAsync(caseId, user!))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new FailureResponseModel
                {
                    Detail = "You don't have permission to add messages to this case"
                });
            }

            // create the message
            var messageDoc = await _messageService.CreateMessageAsync(
                caseId: caseId,
                subject: request.Subject,
                content: request.Content,
                senderId: user!.Id,
                isUrgent: request.IsUrgent
            );

            this.Logger.LogInformation(
                "Message {MessageId} created in case {CaseId} by user {UserId}",
                messageDoc.Id, caseId, user.Id
            );

            // get read-by details (should be empty for new message)
            var readByDetails = await _messageService.GetReadByDetailsAsync(messageDoc.Id);

            return StatusCode(StatusCodes.Status201Created, new MessageResponseModel
            {
                Id = messageDoc.Id,
                CreatedBy = messageDoc.CreatedByUserId,
                CreatedAt = messageDoc.CreatedAtUtc,
                Subject = messageDoc.Subject,
                Content = messageDoc.Content,
                SenderId = messageDoc.SenderUserId,
                IsUrgent = messageDoc.IsUrgent,
                ReadBy = readByDetails
            });
        }
        catch (KeyNotFoundException ex)
        {
            this.Logger.LogWarning(ex, "Case not found: {CaseId}", caseId);
            return StatusCode(StatusCodes.Status404NotFound, new FailureResponseModel { Detail = "Case not found" });
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Failed to create message in case {CaseId}", caseId);
            return StatusCode(StatusCodes.Status500InternalServerError, new FailureResponseModel
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
    public async Task<ActionResult<List<MessageResponseModel>>> GetMessages(Guid caseId)
    {
        try
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            if (!await _caseDocService.CheckUserAccessAsync(caseId, user!))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new FailureResponseModel
                {
                    Detail = "You don't have permission to view messages in this case"
                });
            }

            var messages = await _messageService.GetMessagesForCaseAsync(caseId);

            var response = new List<MessageResponseModel>();

            foreach (var msg in messages)
            {
                var readByDetails = await _messageService.GetReadByDetailsAsync(msg.Id);

                response.Add(new MessageResponseModel
                {
                    Id = msg.Id,
                    CreatedAt = msg.CreatedAtUtc,
                    CreatedBy = msg.CreatedByUserId,
                    Subject = msg.Subject,
                    Content = msg.Content,
                    SenderId = msg.SenderUserId,
                    IsUrgent = msg.IsUrgent,
                    ReadBy = readByDetails
                });
            }

            return StatusCode(StatusCodes.Status200OK, response);
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Failed to get messages for case {CaseId}", caseId);
            return StatusCode(StatusCodes.Status500InternalServerError, new FailureResponseModel
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
        Guid caseId,
        Guid messageId
    )
    {
        try
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            if (!await _caseDocService.CheckUserAccessAsync(caseId, user!))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new FailureResponseModel
                {
                    Detail = "You don't have permission to view messages in this case"
                });
            }

            var messageDoc = await _messageService.GetMessageAsync(messageId);
            if (messageDoc == null)
            {
                return StatusCode(StatusCodes.Status404NotFound, new FailureResponseModel { Detail = "Message not found" });
            }

            // verify the message belongs to this case
            if (messageDoc.CaseId != caseId)
            {
                return StatusCode(StatusCodes.Status404NotFound, new FailureResponseModel { Detail = "Message not found in this case" });
            }

            // mark the message as read
            await _messageService.MarkAsReadAsync(messageId, user!.Id);

            // get read-by details
            var readByDetails = await _messageService.GetReadByDetailsAsync(messageId);

            return StatusCode(StatusCodes.Status200OK, new MessageResponseModel
            {
                Id = messageDoc.Id,
                CreatedAt = messageDoc.CreatedAtUtc,
                CreatedBy = messageDoc.CreatedByUserId,
                Subject = messageDoc.Subject,
                Content = messageDoc.Content,
                SenderId = messageDoc.SenderUserId,
                IsUrgent = messageDoc.IsUrgent,
                ReadBy = readByDetails
            });
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Failed to get message {MessageId}", messageId);
            return StatusCode(StatusCodes.Status500InternalServerError, new FailureResponseModel
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
        Guid caseId,
        Guid messageId
    )
    {
        try
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            // check if the user is worker or admin
            var caseDoc = await _caseDocService.GetDocumentAsync(caseId);
            if (caseDoc == null)
            {
                return StatusCode(StatusCodes.Status404NotFound, new FailureResponseModel { Detail = "Case not found" });
            }

            var isWorker = caseDoc.Workers?.Any(w => w.UserId == user!.Id) ?? false;
            if (!user!.IsAdministrator && !isWorker)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new FailureResponseModel
                {
                    Detail = "Only case workers and admins can delete messages"
                });
            }

            // verify the message actually exists and belongs to case
            var message = await _messageService.GetMessageAsync(messageId);
            if (message == null)
            {
                return StatusCode(StatusCodes.Status404NotFound, new FailureResponseModel { Detail = "Message not found" });
            }

            if (message.CaseId != caseId)
            {
                return StatusCode(StatusCodes.Status404NotFound, new FailureResponseModel { Detail = "Message not found in this case" });
            }

            await _messageService.DeleteMessageAsync(messageId);

            this.Logger.LogInformation(
                "Message {MessageId} deleted from case {CaseId} by user {UserId}",
                messageId, caseId, user.Id
            );

            return StatusCode(StatusCodes.Status200OK, new SuccessResponseModel());
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Failed to delete message {MessageId}", messageId);
            return StatusCode(StatusCodes.Status500InternalServerError, new FailureResponseModel
            {
                Detail = "Failed to delete message"
            });
        }
    }
}
