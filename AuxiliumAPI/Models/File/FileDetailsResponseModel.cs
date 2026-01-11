using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AuxiliumAPI.Models.File
{
    public class FileDetailsResponseModel
    {
        [Required]
        [JsonPropertyName("id")]
        public required Guid Id { get; init; }

        [Required]
        [JsonPropertyName("createdAt")]
        public required DateTime CreatedAt { get; init; }

        [Required]
        [JsonPropertyName("createdBy")]
        public required Guid? CreatedBy { get; init; }



        [Required]
        [JsonPropertyName("filename")]
        public required string? Filename { get; init; }

        [Required]
        [JsonPropertyName("description")]
        public required string? Description { get; init; }



        [Required]
        [JsonPropertyName("contentType")]
        public required string ContentType { get; init; }

        [Required]
        [JsonPropertyName("hash")]
        public required string Hash { get; init; }

        [Required]
        [JsonPropertyName("size")]
        public required long Size { get; init; }
    }
}
