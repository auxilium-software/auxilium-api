using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AuxiliumSoftware.AuxiliumServices.API.Models.Me
{
    public class PasswordUpdateRequestModel
    {
        [Required]
        [JsonPropertyName("currentPasswordSha512")]
        public required string CurrentPasswordSha512 { get; init; }

        [Required]
        [JsonPropertyName("newPasswordSha512")]
        public required string NewPasswordSha512 { get; init; }
    }
}
