using CouchDB.Driver.Types;

namespace AuxiliumAPI.Common.Services.Interfaces
{
    public interface ICouchDbService
    {
        Task<string> SaveDocumentAsync<T>(string databaseName, T document) where T : CouchDocument;
        Task<T?> GetDocumentAsync<T>(string databaseName, string documentId) where T : CouchDocument;
        Task DeleteDocumentAsync(string databaseName, string documentId);
    }
}
