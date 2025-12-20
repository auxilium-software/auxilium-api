using AuxiliumAPI.Common.DataStructures.CouchDB;
using AuxiliumAPI.Common.DataStructures.CouchDB.SubStructures;
using AuxiliumAPI.Common.DataStructures.MariaDB;

namespace AuxiliumAPI.Common.Services.Interfaces
{
    public interface IUserDocumentService
    {
        Task<UserDocumentStructure?> GetDocumentAsync(Guid userId);
        Task SaveDocumentAsync(UserDocumentStructure caseDoc);
        Task<Dictionary<string, object>> GetAdditionalPropertiesAsync(Guid userId);
        Task SaveAdditionalPropertyAsync(Guid userId, string propertyName, AdditionalPropertySubStructure propertyStructure);
        Task DeleteAdditionalPropertyAsync(Guid userId, string propertyName);
        Task<bool> CheckUserAccessAsync(Guid userId, UserRowStructure currentUser);
    }
}
