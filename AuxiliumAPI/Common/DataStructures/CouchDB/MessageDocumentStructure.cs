using CouchDB.Driver.Types;
using System.Text.Json.Serialization;

namespace AuxiliumAPI.Common.CouchDbDocumentConstruction.Structures;

public class MessageDocumentStructure : CouchDocument
{
    [JsonPropertyName("createdAt")]
    public required DateTime CreatedAt { get; set; }

    [JsonPropertyName("createdBy")]
    public required string CreatedBy { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; }

    [JsonPropertyName("lastUpdatedBy")]
    public string? LastUpdatedBy { get; set; }



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

    public static MessageDocumentStructure Create(
        string id,
        string createdBy,
        string subject,
        string content,
        string senderId,
        bool isUrgent = false
        )
    {
        var now = DateTime.UtcNow;

        return new MessageDocumentStructure
        {
            Id = id,
            CreatedBy = createdBy,
            CreatedAt = now,
            UpdatedAt = now,
            Subject = subject,
            Content = content,
            SenderId = senderId,
            IsUrgent = isUrgent,
            ReadBy = new()
        };
    }
}
