using System.Text.Json.Serialization;

namespace AuxiliumAPI.Common.DataStructures
{
    public class AdditionalPropertySubStructure
    {
        [JsonPropertyName("id")]
        public required Guid Id { get; set; }

        [JsonPropertyName("createdAt")]
        public required DateTime CreatedAt { get; set; }

        [JsonPropertyName("createdBy")]
        public required Guid CreatedBy { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTime? UpdatedAt { get; set; }

        [JsonPropertyName("lastUpdatedBy")]
        public Guid? LastUpdatedBy { get; set; }




        [JsonPropertyName("originalName")]
        public required string OriginalName { get; set; }

        [JsonPropertyName("prettyName")]
        public required string PrettyName { get; set; }

        [JsonPropertyName("urlSlug")]
        public required string UrlSlug { get; set; }

        [JsonPropertyName("content")]
        public required string Content { get; set; }

        [JsonPropertyName("contentType")]
        public required string ContentType { get; set; }
    }
}
