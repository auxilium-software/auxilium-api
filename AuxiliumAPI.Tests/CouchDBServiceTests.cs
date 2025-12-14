
using AuxiliumAPI.Tests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace AuxiliumAPI.Tests
{
    public class CouchDBServiceTests : IClassFixture<ConfigurationFixture>
    {
        private readonly ITestOutputHelper _output;
        private readonly IConfiguration _configuration;

        public CouchDBServiceTests(
            ITestOutputHelper output,
            ConfigurationFixture fixture
            )
        {
            _output = output;
            _configuration = fixture.Configuration;
        }


        [Fact]
        public async Task CouchDB_WithBasicAuth_ShouldAuthenticate()
        {
            var protocol = this._configuration!["Databases:CouchDB:Protocol"]!;
            var hostname = this._configuration!["Databases:CouchDB:Host"]!;
            var port     = this._configuration!.GetValue<int>("Databases:CouchDB:Port");
            var username = this._configuration!["Databases:CouchDB:Username"]!;
            var password = this._configuration!["Databases:CouchDB:Password"]!;

            var baseUrl = $"{protocol}://{hostname}:{port}";

            using var httpClient = new HttpClient();

            var authBytes = Encoding.ASCII.GetBytes($"{username}:{password}");
            var authHeader = Convert.ToBase64String(authBytes);
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authHeader);

            _output.WriteLine($"Testing: {baseUrl}/_all_dbs");
            _output.WriteLine($"Auth header: Basic {authHeader.Substring(0, 10)}...");

            var response = await httpClient.GetAsync($"{baseUrl}/_all_dbs");
            var content = await response.Content.ReadAsStringAsync();

            _output.WriteLine($"Status: {response.StatusCode}");
            _output.WriteLine($"Content: {content}");

            if (!response.IsSuccessStatusCode)
            {
                _output.WriteLine($"Authentication failed!");
                _output.WriteLine($"Headers sent: {string.Join(", ", httpClient.DefaultRequestHeaders.Select(h => h.Key))}");
            }
            else
            {
                _output.WriteLine($"Authentication successful!");
            }

            response.IsSuccessStatusCode.Should().BeTrue($"Should authenticate successfully but got {response.StatusCode}: {content}");
        }

        [Fact]
        public async Task CouchDB_CheckSpecificDatabase_ShouldExist()
        {
            var protocol = this._configuration!["Databases:CouchDB:Protocol"]!;
            var hostname = this._configuration!["Databases:CouchDB:Host"]!;
            var port = this._configuration!.GetValue<int>("Databases:CouchDB:Port");
            var username = this._configuration!["Databases:CouchDB:Username"]!;
            var password = this._configuration!["Databases:CouchDB:Password"]!;
            var databaseName = this._configuration!["Databases:CouchDB:Databases:Cases"]!;

            var baseUrl = $"{protocol}://{hostname}:{port}";

            using var httpClient = new HttpClient();

            var authBytes = Encoding.ASCII.GetBytes($"{username}:{password}");
            var authHeader = Convert.ToBase64String(authBytes);
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authHeader);

            _output.WriteLine($"Checking if database exists: {databaseName}");

            var response = await httpClient.GetAsync($"{baseUrl}/{databaseName}");
            var content = await response.Content.ReadAsStringAsync();

            _output.WriteLine($"Status: {response.StatusCode}");
            _output.WriteLine($"Content: {content}");

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _output.WriteLine($"Database '{databaseName}' does not exist!");
                _output.WriteLine($"You need to create it first.");
            }
            else if (response.IsSuccessStatusCode)
            {
                _output.WriteLine($"Database '{databaseName}' exists!");
            }
            else
            {
                _output.WriteLine($"Unexpected status: {response.StatusCode}");
            }
        }

        [Fact]
        public async Task CouchDB_GetDocument_ShouldReturnQuickly()
        {
            var protocol = this._configuration!["Databases:CouchDB:Protocol"]!;
            var hostname = this._configuration!["Databases:CouchDB:Host"]!;
            var port = this._configuration!.GetValue<int>("Databases:CouchDB:Port");
            var username = this._configuration!["Databases:CouchDB:Username"]!;
            var password = this._configuration!["Databases:CouchDB:Password"]!;

            var baseUrl = $"{protocol}://{hostname}:{port}";

            var databaseName = "auxilium_users";
            var documentId = "test-nonexistent-doc";

            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(10);

            var authBytes = Encoding.ASCII.GetBytes($"{username}:{password}");
            var authHeader = Convert.ToBase64String(authBytes);
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authHeader);

            _output.WriteLine($"Fetching document: {databaseName}/{documentId}");

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            var response = await httpClient.GetAsync($"{baseUrl}/{databaseName}/{documentId}");
            stopwatch.Stop();

            var content = await response.Content.ReadAsStringAsync();

            _output.WriteLine($"Time: {stopwatch.ElapsedMilliseconds}ms");
            _output.WriteLine($"Status: {response.StatusCode}");
            _output.WriteLine($"Content: {content}");

            stopwatch.ElapsedMilliseconds.Should().BeLessThan(2000,
                "Document fetch should be fast even if not found");

            if (stopwatch.ElapsedMilliseconds > 1000)
            {
                _output.WriteLine($"Slow response: {stopwatch.ElapsedMilliseconds}ms");
            }
            else
            {
                _output.WriteLine($"Fast response: {stopwatch.ElapsedMilliseconds}ms");
            }
        }

        [Fact]
        public async Task CouchDB_FullRoundTrip_ShouldWork()
        {
            var protocol = this._configuration!["Databases:CouchDB:Protocol"]!;
            var hostname = this._configuration!["Databases:CouchDB:Host"]!;
            var port = this._configuration!.GetValue<int>("Databases:CouchDB:Port");
            var username = this._configuration!["Databases:CouchDB:Username"]!;
            var password = this._configuration!["Databases:CouchDB:Password"]!;
            var databaseName = this._configuration!["Databases:CouchDB:Databases:Cases"]!;

            var baseUrl = $"{protocol}://{hostname}:{port}";

            var testDocId = $"test-doc-{Guid.NewGuid()}";

            using var httpClient = new HttpClient();

            var authBytes = Encoding.ASCII.GetBytes($"{username}:{password}");
            var authHeader = Convert.ToBase64String(authBytes);
            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", authHeader);

            try
            {
                _output.WriteLine($"Step 1: Creating document {testDocId}");

                var testDoc = $$"""
            {
                "_id": "{{testDocId}}",
                "type": "test",
                "createdAt": "{{DateTime.UtcNow:O}}"
            }
            """;

                var createResponse = await httpClient.PutAsync(
                    $"{baseUrl}/{databaseName}/{testDocId}",
                    new StringContent(testDoc, Encoding.UTF8, "application/json")
                );

                var createContent = await createResponse.Content.ReadAsStringAsync();
                _output.WriteLine($"Create status: {createResponse.StatusCode}");
                _output.WriteLine($"Create response: {createContent}");

                createResponse.IsSuccessStatusCode.Should().BeTrue("Document creation should succeed");

                _output.WriteLine($"\nStep 2: Reading document {testDocId}");

                var readResponse = await httpClient.GetAsync($"{baseUrl}/{databaseName}/{testDocId}");
                var readContent = await readResponse.Content.ReadAsStringAsync();

                _output.WriteLine($"Read status: {readResponse.StatusCode}");
                _output.WriteLine($"Read response: {readContent}");

                readResponse.IsSuccessStatusCode.Should().BeTrue("Document read should succeed");
                readContent.Should().Contain(testDocId);

                _output.WriteLine($"\nStep 3: Deleting document {testDocId}");

                var readJson = System.Text.Json.JsonDocument.Parse(readContent);
                var rev = readJson.RootElement.GetProperty("_rev").GetString();

                var deleteResponse = await httpClient.DeleteAsync(
                    $"{baseUrl}/{databaseName}/{testDocId}?rev={rev}"
                );

                var deleteContent = await deleteResponse.Content.ReadAsStringAsync();
                _output.WriteLine($"Delete status: {deleteResponse.StatusCode}");
                _output.WriteLine($"Delete response: {deleteContent}");

                deleteResponse.IsSuccessStatusCode.Should().BeTrue("Document deletion should succeed");

                _output.WriteLine("\nFull round-trip successful!");
            }
            catch (Exception ex)
            {
                _output.WriteLine($"\nTest failed: {ex.Message}");
                throw;
            }
        }
    }
}
