using AuxiliumAPI.Models.Case;
using CouchDB.Driver.Types;
using System.Text.Json.Serialization;

namespace AuxiliumAPI.Common.CouchDbDocumentConstruction.Structures;

public class FileDocumentStructure : CouchDocument
{
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("createdBy")]
    public required string CreatedBy { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; }

    [JsonPropertyName("lastUpdatedBy")]
    public string? LastUpdatedBy { get; set; }




    [JsonPropertyName("filename")]
    public required string Filename { get; set; }

    [JsonPropertyName("description")]
    public required string Description { get; set; }

    [JsonPropertyName("contentType")]
    public required string ContentType { get; set; }

    [JsonPropertyName("hash")]
    public required string Hash { get; set; }

    [JsonPropertyName("size")]
    public long Size { get; set; }



    public static FileDocumentStructure Create(
        string id,
        string createdBy,
        string filename,
        string description,
        string contentType,
        string hash,
        long size
        )
    {
        var now = DateTime.UtcNow;

        return new FileDocumentStructure
        {
            Id = id,
            CreatedBy = createdBy,
            CreatedAt = now,
            UpdatedAt = now,
            Filename = filename,
            Description = description,
            ContentType = contentType,
            Hash = hash,
            Size = size
        };
    }
}
