using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AuxiliumAPI.Models.CaseMessage
{
    public class MessageResponseModel
    {
        [Required]
        [JsonPropertyName("id")]
        public required Guid Id { get; init; }

        [Required]
        [JsonPropertyName("createdBy")]
        public required Guid CreatedBy { get; init; }

        [Required]
        [JsonPropertyName("createdAt")]
        public required DateTime CreatedAt { get; init; }

        [Required]
        [JsonPropertyName("subject")]
        public required string Subject { get; init; }

        [Required]
        [JsonPropertyName("content")]
        public required string Content { get; init; }

        [Required]
        [JsonPropertyName("senderId")]
        public required Guid SenderId { get; init; }

        [Required]
        [JsonPropertyName("isUrgent")]
        public required bool IsUrgent { get; init; }

        [Required]
        [JsonPropertyName("readBy")]
        public required Dictionary<Guid, DateTime> ReadBy { get; init; }
    }
}
