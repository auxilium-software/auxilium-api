using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AuxiliumAPI.Models.AdditionalProperty
{
    public class AdditionalPropertyCreationRequestModel
    {
        [Required]
        [JsonPropertyName("name")]
        public required string Name { get; set; }


        [Required]
        [JsonPropertyName("content")]
        public required string Content { get; set; }


        [Required]
        [JsonPropertyName("content_type")]
        public string? ContentType { get; set; }
    }
}
