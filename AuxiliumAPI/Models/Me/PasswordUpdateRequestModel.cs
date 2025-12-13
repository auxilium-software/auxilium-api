using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AuxiliumAPI.Models.Me
{
    public class PasswordUpdateRequestModel
    {
        [Required]
        [JsonPropertyName("currentPassword")]
        public required string CurrentPassword { get; init; }

        [Required]
        [JsonPropertyName("newPassword")]
        public required string NewPassword { get; init; }
    }
}
