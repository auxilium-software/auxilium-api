using CouchDB.Driver.Types;
using System.Text.Json.Serialization;

namespace AuxiliumAPI.Common.CouchDbDocumentConstruction.Structures;

public class MessageDocumentStructure : CouchDocument
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



    [JsonPropertyName("subject")]
    public required string Subject { get; set; }

    [JsonPropertyName("content")]
    public required string Content { get; set; }

    [JsonPropertyName("senderId")]
    public required string SenderId { get; set; }

    [JsonPropertyName("isUrgent")]
    public required bool IsUrgent { get; set; }

    [JsonPropertyName("readBy")]
    public required Dictionary<string, DateTime> ReadBy { get; set; } = new();
}
