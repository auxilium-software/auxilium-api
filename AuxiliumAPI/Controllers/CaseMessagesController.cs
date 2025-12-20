using AuxiliumAPI.Common.ControllerBases;
using AuxiliumAPI.Common.Services.Interfaces;
using AuxiliumAPI.Models;
using AuxiliumAPI.Models.Case;
using AuxiliumAPI.Models.CaseMessage;
using Microsoft.AspNetCore.Mvc;

namespace AuxiliumAPI.Controllers
{

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
            ILogger<CaseMessagesController> logger,
            IMariaDbService mariaDb)
            : base(mariaDb, logger)
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

                // if the case id isn't a valid id, return bad request
                if (!Guid.TryParse(caseId, out _))
                {
                    return BadRequest(new FailureResponseModel { Detail = "Invalid case ID" });
                }

                // check case access
                if (!await _caseDocService.CheckUserAccessAsync(Guid.Parse(caseId), user!))
                {
                    return StatusCode(403, new FailureResponseModel
                    {
                        Detail = "You don't have permission to add messages to this case"
                    });
                }

                // create a message document
                var messageDoc = await _messageService.CreateMessageAsync(
                    caseId: Guid.Parse(caseId),
                    subject: request.Subject,
                    content: request.Content,
                    senderId: user.id,
                    isUrgent: request.IsUrgent
                );

                _logger.LogInformation(
                    "Message {MessageId} created in case {CaseId} by user {UserId}",
                    messageDoc.Id, caseId, user.id
                );

                // return the created message
                return StatusCode(201, new MessageResponseModel
                {
                    Id = Guid.Parse(messageDoc.Id),
                    CreatedBy = messageDoc.CreatedBy,
                    CreatedAt = messageDoc.CreatedAt,
                    Subject = messageDoc.Subject,
                    Content = messageDoc.Content,
                    SenderId = messageDoc.SenderId,
                    IsUrgent = messageDoc.IsUrgent,
                    ReadBy = messageDoc.ReadBy
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

        [HttpGet("{messageId}")]
        [ProducesResponseType(typeof(MessageResponseModel), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<MessageResponseModel>> GetMessage(
            string caseId,
            string messageId)
        {
            try
            {
                var (user, error) = await GetCurrentUserAsync();
                if (error != null) return error;

                // if the case id or message id isn't a valid id, return bad request
                if (!Guid.TryParse(caseId, out _) || !Guid.TryParse(messageId, out _))
                {
                    return BadRequest(new FailureResponseModel { Detail = "Invalid ID format" });
                }

                // check the user's access to the case
                if (!await _caseDocService.CheckUserAccessAsync(Guid.Parse(caseId), user!))
                {
                    return StatusCode(403, new FailureResponseModel
                    {
                        Detail = "You don't have permission to view messages in this case"
                    });
                }

                // grab the message
                var messageDoc = await _messageService.GetMessageAsync(Guid.Parse(messageId));

                // if the message doesn't exist, return not found
                if (messageDoc == null)
                {
                    return NotFound(new FailureResponseModel { Detail = "Message not found" });
                }

                // mark the message as read by the currently loggged in user
                await _messageService.MarkAsReadAsync(Guid.Parse(messageId), user.id);

                // return
                return Ok(new MessageResponseModel
                {
                    Id = Guid.Parse(messageDoc.Id),
                    CreatedAt = messageDoc.CreatedAt,
                    CreatedBy = messageDoc.CreatedBy,
                    Subject = messageDoc.Subject,
                    Content = messageDoc.Content,
                    SenderId = messageDoc.SenderId,
                    IsUrgent = messageDoc.IsUrgent,
                    ReadBy = messageDoc.ReadBy,
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
    }
}
