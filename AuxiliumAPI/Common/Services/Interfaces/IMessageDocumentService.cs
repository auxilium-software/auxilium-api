using AuxiliumAPI.Common.DataStructures.CouchDB;

namespace AuxiliumAPI.Common.Services.Interfaces
{
    public interface IMessageDocumentService
    {
        Task<MessageDocumentStructure> CreateMessageAsync(
            Guid caseId,
            string subject,
            string content,
            Guid senderId,
            bool isUrgent = false
        );

        Task<MessageDocumentStructure?> GetMessageAsync(Guid messageId);

        Task MarkAsReadAsync(Guid messageId, Guid userId);

        Task<bool> CheckUserAccessAsync(Guid messageId, Guid userId);
    }
}
