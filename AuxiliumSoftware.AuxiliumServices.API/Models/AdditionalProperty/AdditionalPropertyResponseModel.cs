using System.Text.Json.Serialization;

namespace AuxiliumSoftware.AuxiliumServices.API.Models.AdditionalProperty
{
    public class AdditionalPropertyResponseModel
    {
        [JsonPropertyName("urlSlug")]
        public string UrlSlug { get; set; } = string.Empty;

        [JsonPropertyName("originalName")]
        public string OriginalName { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;

        [JsonPropertyName("contentType")]
        public string? ContentType { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("lastUpdatedAt")]
        public DateTime? LastUpdatedAt { get; set; }
    }
}
