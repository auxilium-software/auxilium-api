using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AuxiliumSoftware.AuxiliumServices.API.Models.CaseMessage
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
        public required bool IsUrgent { get; init; }
    }
}
