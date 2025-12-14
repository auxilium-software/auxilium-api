using AuxiliumAPI.Common.CouchDbDocumentConstruction.Structures;
using AuxiliumAPI.Common.DataStructures.CouchDB.SubStructures;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AuxiliumAPI.Models.Case
{
    public class CaseResponseModel
    {
        [Required]
        [JsonPropertyName("id")]
        public required Guid ID { get; init; }

        [Required]
        [JsonPropertyName("createdAt")]
        public required DateTime CreatedAt { get; init; }

        [Required]
        [JsonPropertyName("createdBy")]
        public required Guid CreatedBy { get; init; }

        [Required]
        [JsonPropertyName("lastUpdatedAt")]
        public required DateTime? LastUpdatedAt { get; init; } = null;

        [Required]
        [JsonPropertyName("lastUpdatedBy")]
        public required Guid? LastUpdatedBy { get; init; } = null;



        [Required]
        [JsonPropertyName("title")]
        public required string Title { get; init; }

        [Required]
        [JsonPropertyName("description")]
        public required string Description { get; init; }



        [Required]
        [JsonPropertyName("sensitivity")]
        public required CaseSensitivityEnum Sensitivity { get; init; }

        [Required]
        [JsonPropertyName("status")]
        public required CaseStatusEnum Status { get; init; }



        [Required]
        [JsonPropertyName("workers")]
        public required List<Guid> Workers { get; init; }

        [Required]
        [JsonPropertyName("clients")]
        public required List<Guid> Clients { get; init; }



        [Required]
        [JsonPropertyName("referrer")]
        public required string? Referrer { get; init; }

        [Required]
        [JsonPropertyName("todos")]
        public required Dictionary<string, object> Todos { get; init; }

        [Required]
        [JsonPropertyName("timeline")]
        public required Dictionary<string, object> Timeline { get; init; }

        [Required]
        [JsonPropertyName("messages")]
        public required List<string> Messages { get; init; }



        [Required]
        [JsonPropertyName("additionalProperties")]
        public required Dictionary<string, AdditionalPropertyStructure> AdditionalProperties { get; init; }

        [Required]
        [JsonPropertyName("files")]
        public required List<FileDocumentStructure> Files { get; init; }
    }
}
