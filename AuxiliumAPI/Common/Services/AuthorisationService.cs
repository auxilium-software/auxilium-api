using AuxiliumAPI.Common.EntityModels;
using AuxiliumAPI.Common.Services.Interfaces;

namespace AuxiliumAPI.Common.Services
{
    public class AuthorisationService : IAuthorisationService
    {
        public bool IsAdmin(UserModel user)
        {
            return user.IsAdmin;
        }

        public bool IsCaseWorker(UserModel user)
        {
            return user.IsCaseWorker || user.IsAdmin;
        }

        public bool CanViewCase(UserModel user, CaseModel caseDoc)
        {
            // admins can very everything
            if (user.IsAdmin) return true;

            // workers and clients can view their assigned cases
            var isWorker = caseDoc.Workers?.Any(w => w.UserId == user.Id) ?? false;
            var isClient = caseDoc.Clients?.Any(c => c.UserId == user.Id) ?? false;

            return isWorker || isClient;
        }

        public bool CanModifyCase(UserModel user, CaseModel caseDoc)
        {
            // admins can modify anything
            if (user.IsAdmin) return true;

            // workers can modify their cases
            return caseDoc.Workers?.Any(w => w.UserId == user.Id) ?? false;
        }

        public bool CanDeleteCase(UserModel user, CaseModel caseDoc)
        {
            // same permissions as to modify
            return CanModifyCase(user, caseDoc);
        }

        public bool CanManageCaseProperties(UserModel user, CaseModel caseDoc)
        {
            // same permissions as to modify
            return CanModifyCase(user, caseDoc);
        }

        public bool CanManageTodos(UserModel user, CaseModel caseDoc)
        {
            // same permissions as to modify
            return CanModifyCase(user, caseDoc);
        }

        public bool CanViewUser(UserModel currentUser, Guid targetUserId)
        {
            // admins can view anyone
            if (currentUser.IsAdmin) return true;

            // users can view themselves
            return currentUser.Id == targetUserId;
        }

        public bool CanModifyUser(UserModel currentUser, Guid targetUserId)
        {
            // only admins can modify other users
            if (currentUser.IsAdmin) return true;

            // users can modify themselves
            return currentUser.Id == targetUserId;
        }
    }
}
