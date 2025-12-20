using AuxiliumAPI.Common.DataStructures.CouchDB;
using AuxiliumAPI.Common.DataStructures.CouchDB.SubStructures;
using AuxiliumAPI.Common.DataStructures.MariaDB;
using AuxiliumAPI.Models.Case;

namespace AuxiliumAPI.Common.Services.Interfaces
{
    public interface ICaseDocumentService
    {
        Task<CaseDocumentStructure?> GetDocumentAsync(Guid caseId);
        Task SaveDocumentAsync(CaseDocumentStructure caseDoc);



        Task AddClientAsync(Guid caseId, Guid userId);
        Task RemoveClientAsync(Guid caseId, Guid userId);
        Task AddWorkerAsync(Guid caseId, Guid userId);
        Task RemoveWorkerAsync(Guid caseId, Guid userId);



        Task<Dictionary<string, object>> GetAdditionalPropertiesAsync(Guid caseId);
        Task SaveAdditionalPropertyAsync(Guid caseId, string propertyName, AdditionalPropertySubStructure propertyStructure);
        Task DeleteAdditionalPropertyAsync(Guid caseId, string propertyName);



        Task<CaseTodoSubStructure> CreateTodoAsync(
            Guid caseId,
            string summary,
            string? description,
            TodoPriorityEnum priority,
            Guid createdBy,
            DateTime? dueDate = null,
            Guid? assignedTo = null,
            DateTime? reminder = null
        );



        Task<bool> CheckUserAccessAsync(Guid caseId, UserRowStructure currentUser);
    }
}
