using AuxiliumAPI.Common.EF;
using AuxiliumAPI.Common.EntityModels;
using AuxiliumAPI.Common.Enumerators;
using AuxiliumAPI.Common.Services.Interfaces;
using AuxiliumAPI.Common.Utilities;
using Microsoft.EntityFrameworkCore;

namespace AuxiliumAPI.Common.Services;

/// <summary>
/// Service for case message operations
/// </summary>
public class MessageDocumentService : IMessageDocumentService
{
    private readonly AuxiliumDbContext _db;
    private readonly ILogger<MessageDocumentService> _logger;

    public MessageDocumentService(
        AuxiliumDbContext db,
        ILogger<MessageDocumentService> logger)
    {
        _db = db;
        _logger = logger;
    }

    #region ========================= MESSAGE OPERATIONS =========================
    public async Task<CaseMessageModel> CreateMessageAsync(
        Guid caseId,
        string subject,
        string content,
        Guid senderId,
        bool isUrgent = false)
    {
        try
        {
            // verify the case actually exists
            var caseExists = await _db.Cases.AnyAsync(c => c.Id == caseId);
            if (!caseExists)
            {
                throw new KeyNotFoundException($"Case {caseId} not found");
            }

            // create the message entity
            var message = new CaseMessageModel
            {
                Id = UUIDUtilities.GenerateV5(DatabaseObjectType.Message),
                CaseId = caseId,
                SenderId = senderId,
                Subject = subject,
                Content = content,
                IsUrgent = isUrgent,
                CreatedBy = senderId,
                CreatedAt = DateTime.UtcNow,
                LastUpdatedAt = DateTime.UtcNow,
                LastUpdatedBy = senderId
            };

            _db.CaseMessages.Add(message);

            // update the LastUpdatedAt timestamp for the case
            var caseEntity = await _db.Cases.FindAsync(caseId);
            if (caseEntity != null)
            {
                caseEntity.LastUpdatedAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();

            _logger.LogInformation("Created message {MessageId} in case {CaseId}", message.Id, caseId);

            // TODO: Send notification using RabbitMQ

            return message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create message in case {CaseId}", caseId);
            throw;
        }
    }

    public async Task<CaseMessageModel?> GetMessageAsync(Guid messageId)
    {
        try
        {
            return await _db.CaseMessages
                .Include(m => m.Sender)
                .Include(m => m.Case)
                .FirstOrDefaultAsync(m => m.Id == messageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get message {MessageId}", messageId);
            return null;
        }
    }

    public async Task<List<CaseMessageModel>> GetMessagesForCaseAsync(Guid caseId)
    {
        try
        {
            return await _db.CaseMessages
                .Include(m => m.Sender)
                .Where(m => m.CaseId == caseId)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get messages for case {CaseId}", caseId);
            throw;
        }
    }

    public async Task MarkAsReadAsync(Guid messageId, Guid userId)
    {
        try
        {
            var message = await GetMessageAsync(messageId)
                ?? throw new KeyNotFoundException($"Message {messageId} not found");

            // check if it's already marked as read
            var alreadyRead = await _db.CaseMessagesReadBys
                .AnyAsync(rb => rb.MessageId == messageId && rb.CreatedBy == userId);

            if (!alreadyRead)
            {
                // create a read-by entry
                var readBy = new CaseMessageReadByModel
                {
                    Id = UUIDUtilities.GenerateV5(DatabaseObjectType.MessageReadBy),
                    MessageId = messageId,
                    CreatedBy = userId,
                    CreatedAt = DateTime.UtcNow
                };

                _db.CaseMessagesReadBys.Add(readBy);

                // update the LastUpdatedAt timestamp for the case
                message.LastUpdatedAt = DateTime.UtcNow;

                await _db.SaveChangesAsync();

                _logger.LogInformation("User {UserId} marked message {MessageId} as read", userId, messageId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark message {MessageId} as read", messageId);
            throw;
        }
    }

    public async Task<bool> IsReadByAsync(Guid messageId, Guid userId)
    {
        try
        {
            return await _db.CaseMessagesReadBys
                .AnyAsync(rb => rb.MessageId == messageId && rb.CreatedBy == userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if message {MessageId} was read", messageId);
            return false;
        }
    }

    public async Task<List<Guid>> GetReadByUsersAsync(Guid messageId)
    {
        try
        {
            return await _db.CaseMessagesReadBys
                .Where(rb => rb.MessageId == messageId)
                .Select(rb => rb.CreatedBy)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get read-by users for message {MessageId}", messageId);
            throw;
        }
    }

    public async Task<Dictionary<Guid, DateTime>> GetReadByDetailsAsync(Guid messageId)
    {
        try
        {
            return await _db.CaseMessagesReadBys
                .Where(rb => rb.MessageId == messageId)
                .ToDictionaryAsync(
                    rb => rb.CreatedBy,
                    rb => rb.CreatedAt
                );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get read-by details for message {MessageId}", messageId);
            throw;
        }
    }

    public async Task DeleteMessageAsync(Guid messageId)
    {
        try
        {
            var message = await _db.CaseMessages.FindAsync(messageId)
                ?? throw new KeyNotFoundException($"Message {messageId} not found");

            // remove the message (read-by entries will cascade delete within the database)
            _db.CaseMessages.Remove(message);

            // update the LastUpdatedAt timestamp for the case
            var caseEntity = await _db.Cases.FindAsync(message.CaseId);
            if (caseEntity != null)
            {
                caseEntity.LastUpdatedAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();

            _logger.LogInformation("Deleted message {MessageId}", messageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete message {MessageId}", messageId);
            throw;
        }
    }
    #endregion
    #region ========================= PERMISSION CHECKS =========================

    public async Task<bool> CheckUserAccessAsync(Guid messageId, UserModel currentUser)
    {
        try
        {
            // get message with case from the database
            var message = await _db.CaseMessages
                .Include(m => m.Case)
                    .ThenInclude(c => c.Workers)
                .Include(m => m.Case)
                    .ThenInclude(c => c.Clients)
                .FirstOrDefaultAsync(m => m.Id == messageId);

            if (message == null) return false;

            // admins have access to everything
            if (currentUser.IsAdmin) return true;

            // check if user is worker or client on the case
            var hasAccess = message.Case.Workers.Any(w => w.UserId == currentUser.Id) ||
                          message.Case.Clients.Any(c => c.UserId == currentUser.Id);

            return hasAccess;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check access for message {MessageId}", messageId);
            return false;
        }
    }
    #endregion
}
