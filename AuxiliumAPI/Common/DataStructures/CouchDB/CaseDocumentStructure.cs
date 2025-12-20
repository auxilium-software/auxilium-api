using AuxiliumAPI.Common.DataStructures.CouchDB.SubStructures;
using AuxiliumAPI.Models.Case;
using CouchDB.Driver.Types;
using System.Text.Json.Serialization;

namespace AuxiliumAPI.Common.DataStructures.CouchDB;


public class CaseDocumentStructure : CouchDocument
{
    [JsonPropertyName("_id")]
    public new string Id
    {
        get => base.Id;
        set => base.Id = value;
    }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("createdBy")]
    public required Guid CreatedBy { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTime LastUpdatedAt { get; set; }

    [JsonPropertyName("lastUpdatedBy")]
    public Guid? LastUpdatedBy { get; set; }



    [JsonPropertyName("title")]
    public required string Title { get; set; }

    [JsonPropertyName("description")]
    public required string Description { get; set; }

    [JsonPropertyName("sensitivity")]
    public CaseSensitivityEnum Sensitivity { get; set; }

    [JsonPropertyName("status")]
    public CaseStatusEnum Status { get; set; }

    [JsonPropertyName("referrer")]
    public string? Referrer { get; set; }

    [JsonPropertyName("workers")]
    public List<Guid> Workers { get; set; } = [];

    [JsonPropertyName("clients")]
    public List<Guid> Clients { get; set; } = [];

    [JsonPropertyName("messages")]
    public List<string> Messages { get; set; } = [];

    [JsonPropertyName("files")]
    public List<string> Files { get; set; } = [];

    [JsonPropertyName("todos")]
    public Dictionary<string, object> Todos { get; set; } = new();

    [JsonPropertyName("timeline")]
    public Dictionary<string, object> Timeline { get; set; } = new();

    [JsonPropertyName("additionalProperties")]
    public Dictionary<string, AdditionalPropertySubStructure> AdditionalProperties { get; set; } = new();
}
