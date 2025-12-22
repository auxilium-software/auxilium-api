using System.Text.Json.Serialization;

namespace AuxiliumAPI.Models.AdditionalProperty
{
    public class AdditionalPropertyCreationRequestModel
    {
        [JsonPropertyName("content")]
        public required string Content { get; set; }


        [JsonPropertyName("display_name")]
        public string? DisplayName { get; set; }


        [JsonPropertyName("content_type")]
        public string? ContentType { get; set; }
    }
}
