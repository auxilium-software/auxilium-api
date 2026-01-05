
using AuxiliumAPI.Common.EntityModels;

namespace AuxiliumAPI.Common.Services.Interfaces
{
    public interface IUserDocumentService
    {
        Task<UserModel?> GetDocumentAsync(Guid userId);
        Task SaveDocumentAsync(UserModel userDoc);



        Task<List<UserAdditionalPropertyModel>> GetAdditionalPropertiesAsync(Guid userId);
        Task SaveAdditionalPropertyAsync(Guid userId, string additionalPropertyName, string additionalPropertyContent);
        Task DeleteAdditionalPropertyAsync(Guid userId, Guid additionalPropertyId);



        Task<bool> CheckUserAccessAsync(Guid userId, UserModel currentUser);

    }
}
