using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AuxiliumSoftware.AuxiliumServices.API.Models.UserLogin
{
    public class ForcedPasswordChangeRequestModel
    {
        [Required]
        [JsonPropertyName("passwordChangeToken")]
        public string PasswordChangeToken { get; set; } = string.Empty;

        [JsonPropertyName("rawPassword")]
        public string? RawPassword { get; set; }
        [JsonPropertyName("passwordSha512")]
        public string? PasswordSha512 { get; set; }
    }
}
