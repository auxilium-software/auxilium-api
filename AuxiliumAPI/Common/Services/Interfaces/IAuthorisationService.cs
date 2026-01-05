using AuxiliumAPI.Common.EntityModels;

namespace AuxiliumAPI.Common.Services.Interfaces
{
    public interface IAuthorisationService
    {
        bool IsAdmin(UserModel user);
        bool IsCaseWorker(UserModel user);
        bool CanViewCase(UserModel user, CaseModel caseDoc);
        bool CanModifyCase(UserModel user, CaseModel caseDoc);
        bool CanDeleteCase(UserModel user, CaseModel caseDoc);
        bool CanManageCaseProperties(UserModel user, CaseModel caseDoc);
        bool CanManageTodos(UserModel user, CaseModel caseDoc);
        bool CanViewUser(UserModel currentUser, Guid targetUserId);
        bool CanModifyUser(UserModel currentUser, Guid targetUserId);
    }
}
