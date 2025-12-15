using AuxiliumAPI.Models.File;
using CouchDB.Driver.Types;
using System.Text.Json.Serialization;

namespace AuxiliumAPI.Common.DataStructures.CouchDB;

public class FileDocumentStructure : CouchDocument
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




    [JsonPropertyName("parentType")]
    public required FileParentTypeEnum ParentType { get; set; }

    [JsonPropertyName("parentId")]
    public required Guid ParentId { get; set; }




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
}
