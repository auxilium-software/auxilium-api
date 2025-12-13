using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AuxiliumAPI.Models.UserLogin
{
    public class UserLoginRequestModel
    {
        [Required]
        [EmailAddress]
        [JsonPropertyName("emailAddress")]
        public required string EmailAddress { get; init; }

        [Required]
        [JsonPropertyName("rawPassword")]
        public required string RawPassword { get; init; }

        [Required]
        [JsonPropertyName("recaptchaToken")]
        public required string RecaptchaToken { get; init; }
    }
}
