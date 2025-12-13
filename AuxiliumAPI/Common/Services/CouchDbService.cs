using AuxiliumAPI.Common.Services.Interfaces;
using AuxiliumAPI.Common.Utilities;
using CouchDB.Driver;
using CouchDB.Driver.Types;
using System.Text.Json;

namespace AuxiliumAPI.Common.Services
{
    public class CouchDbService : ICouchDbService
    {
        private readonly CouchClient CouchDBClient;

        public CouchDbService()
        {
            string protocol = ConfigurationUtilities.GetString("Databases", "CouchDB", "Protocol");
            string hostname = ConfigurationUtilities.GetString("Databases", "CouchDB", "Host");
            int port        = ConfigurationUtilities.GetInteger("Databases", "CouchDB", "Port");
            string username = ConfigurationUtilities.GetString("Databases", "CouchDB", "Username");
            string password = ConfigurationUtilities.GetString("Databases", "CouchDB", "Password");

            var connectionURL = $"{protocol}://{hostname}:{port}/";

            CouchDBClient = new CouchClient(connectionURL, builder => builder
                .UseBasicAuthentication(username, password)
            );
        }

        public async Task<string> SaveDocumentAsync<T>(string databaseName, T document) where T : CouchDocument
        {
            var db = CouchDBClient.GetDatabase<T>(databaseName);
            var result = await db.AddOrUpdateAsync(document);
            return result.Id;
        }

        public async Task<T?> GetDocumentAsync<T>(string databaseName, string documentId) where T : CouchDocument
        {
            try
            {
                var db = CouchDBClient.GetDatabase<T>(databaseName);
                var document = await db.FindAsync(documentId);
                return document;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task DeleteDocumentAsync(string databaseName, string documentId)
        {
            try
            {
                var db = CouchDBClient.GetDatabase<CouchDocument>(databaseName);
                var doc = await db.FindAsync(documentId);
                if (doc != null)
                {
                    await db.RemoveAsync(doc);
                }
            }
            catch (Exception)
            {
            }
        }
    }
}
