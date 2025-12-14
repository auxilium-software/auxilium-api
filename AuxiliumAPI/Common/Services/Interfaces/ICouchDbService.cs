using AuxiliumAPI.Common.DataStructures.Internal;
using CouchDB.Driver.Types;

namespace AuxiliumAPI.Common.Services.Interfaces;

public interface ICouchDbService
{
    Task<string> SaveDocumentAsync<T>(string databaseName, T document) where T : CouchDocument;
    Task<T?> GetDocumentAsync<T>(string databaseName, string documentId) where T : CouchDocument;
    Task DeleteDocumentAsync(string databaseName, string documentId);
    Task<CouchDBQueryResult<T>> QueryAsync<T>(
        string databaseName,
        object selector,
        int limit = 25,
        int skip = 0,
        string[]? sort = null) where T : CouchDocument;
    Task<int> CountAsync<T>(string databaseName, object selector) where T : CouchDocument;
}
