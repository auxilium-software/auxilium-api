
using AuxiliumAPI.Common.EntityModels;

namespace AuxiliumAPI.Common.Services.Interfaces
{
    public interface IMessageDocumentService
    {
        Task<CaseMessageModel> CreateMessageAsync(
                Guid caseId,
                string subject,
                string content,
                Guid senderId,
                bool isUrgent = false);

        Task<CaseMessageModel?> GetMessageAsync(Guid messageId);
        Task<List<CaseMessageModel>> GetMessagesForCaseAsync(Guid caseId);
        Task MarkAsReadAsync(Guid messageId, Guid userId);
        Task<bool> IsReadByAsync(Guid messageId, Guid userId);
        Task DeleteMessageAsync(Guid messageId);
        Task<bool> CheckUserAccessAsync(Guid messageId, UserModel currentUser);
    }
}
