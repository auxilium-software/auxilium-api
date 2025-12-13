using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AuxiliumAPI.Models.Case
{
    public class MessageCreationRequestModel
    {
        [Required]
        [JsonPropertyName("subject")]
        public required string Subject { get; init; }

        [Required]
        [JsonPropertyName("content")]
        public required string Content { get; init; }

        [Required]
        [JsonPropertyName("isUrgent")]
        public required string IsUrgent { get; init; }
    }
}
