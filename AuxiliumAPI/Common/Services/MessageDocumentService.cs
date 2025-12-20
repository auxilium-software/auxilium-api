using AuxiliumAPI.Common.DataStructures.CouchDB;
using AuxiliumAPI.Common.Enumerators;
using AuxiliumAPI.Common.Services.Interfaces;
using AuxiliumAPI.Common.Utilities;

namespace AuxiliumAPI.Common.Services
{
    public class MessageDocumentService : IMessageDocumentService
    {
        private readonly ICouchDbService _couchDb;
        private readonly ICaseDocumentService _caseDocService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<MessageDocumentService> _logger;
        private readonly string _messagesDatabaseName;

        public MessageDocumentService(
            ICouchDbService couchDb,
            ICaseDocumentService caseDocService,
            IConfiguration configuration,
            ILogger<MessageDocumentService> logger)
        {
            _couchDb = couchDb;
            _caseDocService = caseDocService;
            _configuration = configuration;
            _logger = logger;

            _messagesDatabaseName = _configuration["Databases:CouchDB:Databases:Messages"]
                ?? throw new InvalidOperationException("Messages database not configured");
        }

        public async Task<MessageDocumentStructure> CreateMessageAsync(
            Guid caseId,
            string subject,
            string content,
            Guid senderId,
            bool isUrgent = false
            )
        {
            try
            {
                // grab the parent case document to make sure it exists
                var caseDoc = await _caseDocService.GetDocumentAsync(caseId)
                    ?? throw new KeyNotFoundException($"Case {caseId} not found");

                // generate a uuid for the message
                Guid messageId = UUIDUtilities.GenerateV5(DatabaseObjectType.Message);

                // create the message document itself
                var messageDoc = new MessageDocumentStructure
                {
                    Id = messageId.ToString(),
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = senderId,

                    ParentType = Models.File.MessageParentTypeEnum.Case,
                    ParentId = caseId,

                    Subject = subject,
                    Content = content,
                    SenderId = senderId,
                    IsUrgent = isUrgent,
                    LastUpdatedAt = null,
                    LastUpdatedBy = null,
                    ReadBy = new Dictionary<Guid, DateTime>()
                };

                // save the message document to couchdb
                await _couchDb.SaveDocumentAsync(_messagesDatabaseName, messageDoc);

                // add a reference to the message to the case document
                caseDoc.Messages ??= new List<string>();
                caseDoc.Messages.Add($"auxmsg://localhost/message/{messageId}");
                caseDoc.LastUpdatedAt = DateTime.UtcNow;

                // save the case document to couchdb
                await _caseDocService.SaveDocumentAsync(caseDoc);

                // TODO: send a notification using rabbitmq

                _logger.LogInformation(
                    "Created message {MessageId} in case {CaseId}",
                    messageId, caseId
                );

                // return
                return messageDoc;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create message in case {CaseId}", caseId);
                throw;
            }
        }

        public async Task<MessageDocumentStructure?> GetMessageAsync(Guid messageId)
        {
            try
            {
                return await _couchDb.GetDocumentAsync<MessageDocumentStructure>(
                    _messagesDatabaseName,
                    messageId
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get message {MessageId}", messageId);
                return null;
            }
        }

        public async Task MarkAsReadAsync(Guid messageId, Guid userId)
        {
            try
            {
                // grab the message doc
                var messageDoc = await GetMessageAsync(messageId)
                    ?? throw new KeyNotFoundException($"Message {messageId} not found");

                // if the user hasn't read it yet, mark it as read
                if (!messageDoc.ReadBy.ContainsKey(userId))
                {
                    messageDoc.ReadBy[userId] = DateTime.UtcNow;
                    messageDoc.LastUpdatedAt = DateTime.UtcNow;

                    await _couchDb.SaveDocumentAsync(_messagesDatabaseName, messageDoc);

                    _logger.LogInformation(
                        "User {UserId} marked message {MessageId} as read",
                        userId, messageId
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to mark message {MessageId} as read", messageId);
                throw;
            }
        }

        public async Task<bool> CheckUserAccessAsync(Guid messageId, Guid userId)
        {
            try
            {
                // grab the message
                var messageDoc = await GetMessageAsync(messageId);

                // if the message doc doesn't exist, return false
                if (messageDoc == null)
                    return false;

                // TODO: implement this lol

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check access for message {MessageId}", messageId);
                return false;
            }
        }
    }
}
