using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AuxiliumSoftware.AuxiliumServices.API.Models.UserLogin
{
    public class InitialSetPasswordRequestModel
    {
        [Required]
        [JsonPropertyName("token")]
        public required string Token { get; set; }

        [JsonPropertyName("rawPassword")]
        public string? RawPassword { get; set; }
        [JsonPropertyName("passwordSha512")]
        public string? PasswordSha512 { get; set; }
    }
}
