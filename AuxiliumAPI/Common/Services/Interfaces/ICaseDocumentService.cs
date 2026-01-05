using AuxiliumAPI.Common.EntityModels;
using AuxiliumAPI.Common.Enumerators;
using AuxiliumAPI.Models.Case;

namespace AuxiliumAPI.Common.Services.Interfaces
{
    public interface ICaseDocumentService
    {
        Task<CaseModel?> GetDocumentAsync(Guid caseId);
        Task SaveDocumentAsync(CaseModel caseDoc);
        Task<bool> CheckUserAccessAsync(Guid caseId, UserModel currentUser);



        Task AddClientAsync(Guid caseId, Guid userId);
        Task RemoveClientAsync(Guid caseId, Guid userId);
        Task AddWorkerAsync(Guid caseId, Guid userId);
        Task RemoveWorkerAsync(Guid caseId, Guid userId);



        Task<List<CaseAdditionalPropertyModel>> GetAdditionalPropertiesAsync(Guid caseId);
        Task SaveAdditionalPropertyAsync(Guid caseId, string additionalPropertyName, string additionalPropertyContent);
        Task DeleteAdditionalPropertyAsync(Guid caseId, Guid additionalPropertyId);



        Task<CaseTodoModel> CreateTodoAsync(
            Guid caseId,
            string summary,
            string? description,
            TodoPriorityEnum priority,
            Guid createdBy,
            DateTime? dueDate = null,
            Guid? assignedTo = null,
            DateTime? reminder = null);
        Task<CaseTodoModel?> GetTodoAsync(Guid caseId, Guid todoId);
        Task<List<CaseTodoModel>> GetTodosAsync(Guid caseId);
        Task UpdateTodoStatusAsync(
            Guid caseId,
            Guid todoId,
            TodoStatusEnum status,
            Guid? completedBy = null,
            string? completionNotes = null);
        Task UpdateTodoAsync(
            Guid caseId,
            Guid todoId,
            string? summary = null,
            string? description = null,
            TodoPriorityEnum? priority = null,
            DateTime? dueDate = null,
            Guid? assignedTo = null,
            DateTime? reminder = null);
        Task DeleteTodoAsync(Guid caseId, Guid todoId);
    }
}
