using AuxiliumAPI.Common.DataStructures.CouchDB;
using AuxiliumAPI.Common.DataStructures.CouchDB.SubStructures;
using AuxiliumAPI.Common.DataStructures.MariaDB;
using AuxiliumAPI.Common.Services.Interfaces;

namespace AuxiliumAPI.Common.Services
{
    public class CaseDocumentService : ICaseDocumentService
    {
        private readonly ICouchDbService _couchDb;
        private readonly IConfiguration _configuration;
        private readonly ILogger<CaseDocumentService> _logger;
        private readonly string _casesDatabaseName;

        public CaseDocumentService(
            ICouchDbService couchDb,
            IConfiguration configuration,
            ILogger<CaseDocumentService> logger
            )
        {
            _couchDb = couchDb;
            _configuration = configuration;
            _logger = logger;
            _casesDatabaseName = _configuration["Databases:CouchDB:Databases:Cases"]!;
        }

        public async Task<CaseDocumentStructure?> GetDocumentAsync(Guid caseId)
        {
            return await _couchDb.GetDocumentAsync<CaseDocumentStructure>(_casesDatabaseName, caseId);
        }

        public async Task SaveDocumentAsync(CaseDocumentStructure caseDoc)
        {
            caseDoc.LastUpdatedAt = DateTime.UtcNow;
            await _couchDb.SaveDocumentAsync(_casesDatabaseName, caseDoc);
        }

        public async Task AddClientAsync(Guid caseId, Guid userId)
        {
            var caseDoc = await GetDocumentAsync(caseId) ?? throw new KeyNotFoundException($"Case {caseId} not found");

            if (!caseDoc.Clients.Contains(userId))
            {
                caseDoc.Clients.Add(userId);
                await SaveDocumentAsync(caseDoc);
            }
        }

        public async Task RemoveClientAsync(Guid caseId, Guid userId)
        {
            var caseDoc = await GetDocumentAsync(caseId) ?? throw new KeyNotFoundException($"Case {caseId} not found");

            caseDoc.Clients.Remove(userId);
            await SaveDocumentAsync(caseDoc);
        }

        public async Task AddWorkerAsync(Guid caseId, Guid userId)
        {
            var caseDoc = await GetDocumentAsync(caseId) ?? throw new KeyNotFoundException($"Case {caseId} not found");

            if (!caseDoc.Workers.Contains(userId))
            {
                caseDoc.Workers.Add(userId);
                await SaveDocumentAsync(caseDoc);
            }
        }

        public async Task RemoveWorkerAsync(Guid caseId, Guid userId)
        {
            var caseDoc = await GetDocumentAsync(caseId) ?? throw new KeyNotFoundException($"Case {caseId} not found");

            caseDoc.Workers.Remove(userId);
            await SaveDocumentAsync(caseDoc);
        }

        public async Task<Dictionary<string, object>> GetAdditionalPropertiesAsync(Guid caseId)
        {
            var caseDoc = await GetDocumentAsync(caseId) ?? throw new KeyNotFoundException($"Case {caseId} not found");

            return caseDoc.AdditionalProperties.ToDictionary(
                kvp => kvp.Key,
                kvp => (object)kvp.Value
            );
        }

        public async Task SaveAdditionalPropertyAsync(Guid caseId, string propertyName, AdditionalPropertyStructure propertyStructure)
        {
            var caseDoc = await GetDocumentAsync(caseId) ?? throw new KeyNotFoundException($"Case {caseId} not found");
            caseDoc.AdditionalProperties[propertyName] = propertyStructure;
            await SaveDocumentAsync(caseDoc);
        }

        public async Task DeleteAdditionalPropertyAsync(Guid caseId, string propertyName)
        {
            var caseDoc = await GetDocumentAsync(caseId) ?? throw new KeyNotFoundException($"Case {caseId} not found");

            caseDoc.AdditionalProperties.Remove(propertyName);
            await SaveDocumentAsync(caseDoc);
        }

        public async Task<bool> CheckUserAccessAsync(Guid caseId, UserRowStructure currentUser)
        {
            var caseDoc = await GetDocumentAsync(caseId);
            if (caseDoc == null) return false;

            // admins have access to everything
            if (currentUser.is_admin) return true;

            // check if the user is a client or worker
            var userGuid = currentUser.id;
            return caseDoc.Clients.Contains(userGuid) || caseDoc.Workers.Contains(userGuid);
        }
    }
}
