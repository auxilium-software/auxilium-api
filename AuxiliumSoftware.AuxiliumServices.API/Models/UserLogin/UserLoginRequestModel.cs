using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AuxiliumSoftware.AuxiliumServices.API.Models.UserLogin
{
    public class UserLoginRequestModel
    {
        [Required]
        [EmailAddress]
        [JsonPropertyName("emailAddress")]
        public required string EmailAddress { get; init; }

        [JsonPropertyName("rawPassword")]
        public string? RawPassword { get; init; }

        [JsonPropertyName("passwordSha512")]
        public string? PasswordSha512 { get; set; }

        [Required]
        [JsonPropertyName("recaptchaToken")]
        public required string RecaptchaToken { get; init; }
    }
}
