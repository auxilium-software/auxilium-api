using AuxiliumAPI.Common.DataStructures.CouchDB;
using AuxiliumAPI.Common.DataStructures.CouchDB.SubStructures;
using AuxiliumAPI.Common.DataStructures.MariaDB;
using AuxiliumAPI.Common.Services.Interfaces;

namespace AuxiliumAPI.Common.Services
{
    public class UserDocumentService : IUserDocumentService
    {
        private readonly ICouchDbService _couchDb;
        private readonly IConfiguration _configuration;
        private readonly ILogger<UserDocumentService> _logger;
        private readonly string _usersDatabaseName;

        public UserDocumentService(
            ICouchDbService couchDb,
            IConfiguration configuration,
            ILogger<UserDocumentService> logger
            )
        {
            _couchDb = couchDb;
            _configuration = configuration;
            _logger = logger;
            _usersDatabaseName = _configuration["Databases:CouchDB:Databases:Users"]!;
        }

        public async Task<UserDocumentStructure?> GetDocumentAsync(Guid userId)
        {
            return await _couchDb.GetDocumentAsync<UserDocumentStructure>(_usersDatabaseName, userId);
        }

        public async Task SaveDocumentAsync(UserDocumentStructure userDoc)
        {
            userDoc.LastUpdatedAt = DateTime.UtcNow;
            await _couchDb.SaveDocumentAsync(_usersDatabaseName, userDoc);
        }
        public async Task<Dictionary<string, object>> GetAdditionalPropertiesAsync(Guid userId)
        {
            var userDoc = await GetDocumentAsync(userId) ?? throw new KeyNotFoundException($"User {userId} not found");

            return userDoc.AdditionalProperties.ToDictionary(
                kvp => kvp.Key,
                kvp => (object)kvp.Value
            );
        }

        public async Task SaveAdditionalPropertyAsync(Guid userId, string propertyName, AdditionalPropertyStructure propertyStructure)
        {
            var userDoc = await GetDocumentAsync(userId) ?? throw new KeyNotFoundException($"User {userId} not found");
            userDoc.AdditionalProperties[propertyName] = propertyStructure;
            await SaveDocumentAsync(userDoc);
        }

        public async Task DeleteAdditionalPropertyAsync(Guid userId, string propertyName)
        {
            var userDoc = await GetDocumentAsync(userId) ?? throw new KeyNotFoundException($"User {userId} not found");

            userDoc.AdditionalProperties.Remove(propertyName);
            await SaveDocumentAsync(userDoc);
        }

        public async Task<bool> CheckUserAccessAsync(Guid userId, UserRowStructure currentUser)
        {
            var userDoc = await GetDocumentAsync(userId);
            if (userDoc == null) return false;

            // admins have access to everything
            if (currentUser.is_admin) return true;

            // TODO: write proper access control stuff here
            return false;
        }
    }
}
