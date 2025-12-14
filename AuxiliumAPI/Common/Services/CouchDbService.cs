using AuxiliumAPI.Common.DataStructures.Internal;
using AuxiliumAPI.Common.Services.Interfaces;
using AuxiliumAPI.Common.Utilities;
using CouchDB.Driver;
using CouchDB.Driver.Types;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace AuxiliumAPI.Common.Services;

public class CouchDbService : ICouchDbService
{
    private readonly IConfiguration Configuration;
    private readonly ICouchClient _client;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _baseUrl;
    private readonly string _username;
    private readonly string _password;

    public CouchDbService(
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory
        )
    {
        this.Configuration = configuration;
        _httpClientFactory = httpClientFactory;

        string protocol = this.Configuration!["Databases:CouchDB:Protocol"]!;
        string hostname = this.Configuration!["Databases:CouchDB:Host"]!;
        int port = this.Configuration!.GetValue<int>("Databases:CouchDB:Port");
        _username = this.Configuration!["Databases:CouchDB:Username"]!;
        _password = this.Configuration!["Databases:CouchDB:Password"]!;

        _baseUrl = $"{protocol}://{hostname}:{port}";

        _client = new CouchClient(_baseUrl, builder => builder
            .UseBasicAuthentication(_username, _password)
            .ConfigureFlurlClient(settings =>
            {
                settings.Timeout = TimeSpan.FromSeconds(10);
            })
        );
    }

    public async Task<string> SaveDocumentAsync<T>(string databaseName, T document) where T : CouchDocument
    {
        try
        {
            var db = _client.GetDatabase<T>(databaseName);
            var result = await db.AddOrUpdateAsync(document);
            return result.Id;
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<T?> GetDocumentAsync<T>(string databaseName, string documentId) where T : CouchDocument
    {
        try
        {
            var db = _client.GetDatabase<T>(databaseName);
            var document = await db.FindAsync(documentId);

            return document;
        }
        catch (Flurl.Http.FlurlHttpException ex) when (ex.StatusCode == 404)
        {
            return null;
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task DeleteDocumentAsync(string databaseName, string documentId)
    {
        try
        {
            var db = _client.GetDatabase<CouchDocument>(databaseName);
            var doc = await db.FindAsync(documentId);
            if (doc != null)
            {
                await db.RemoveAsync(doc);
            }
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<CouchDBQueryResult<T>> QueryAsync<T>(
        string databaseName,
        object selector,
        int limit = 25,
        int skip = 0,
        string[]? sort = null) where T : CouchDocument
    {
        try
        {
            // create the http client
            var httpClient = _httpClientFactory.CreateClient();

            // add basic auth
            var authBytes = Encoding.ASCII.GetBytes($"{_username}:{_password}");
            var authHeader = Convert.ToBase64String(authBytes);
            httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authHeader);

            // build the couchdb query
            var query = new
            {
                selector,
                limit,
                skip,
                // sort = sort ?? new[] { new { createdAt = "desc" } }
            };

            var queryJson = JsonSerializer.Serialize(query, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            // POST request on the "_find" endpoint
            var url = $"{_baseUrl}/{databaseName}/_find";
            var content = new StringContent(queryJson, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(url, content);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();

            // parse response
            var result = JsonSerializer.Deserialize<CouchDBFindResponse<T>>(responseJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (result == null)
            {
                throw new InvalidOperationException("Failed to deserialize CouchDB response");
            }

            return new CouchDBQueryResult<T>
            {
                Documents = result.Docs ?? new List<T>(),
                Bookmark = result.Bookmark,
                Warning = result.Warning
            };
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<int> CountAsync<T>(string databaseName, object selector) where T : CouchDocument
    {
        try
        {
            // couchdb doesn't have a count endpoint, so we have to kinda bodge it
            // by getting literally all the documents and counting them

            // first we confirm the query is valid before running such a large query
            var result = await QueryAsync<T>(databaseName, selector, limit: 0, skip: 0);

            // then we run the full query with a very large limit to pull everything that matches and count the results locally
            var fullResult = await QueryAsync<T>(databaseName, selector, limit: 1_000_000, skip: 0);

            return fullResult.Documents.Count;
        }
        catch (Exception ex)
        {
            throw;
        }
    }
}
