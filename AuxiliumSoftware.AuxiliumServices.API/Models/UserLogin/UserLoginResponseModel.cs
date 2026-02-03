using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AuxiliumSoftware.AuxiliumServices.API.Models.UserLogin
{
    public class UserLoginResponseModel
    {
        [JsonPropertyName("accessToken")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("refreshToken")]
        public string? RefreshToken { get; set; }

        [JsonPropertyName("expiresIn")]
        public int? ExpiresIn { get; set; }



        [Required]
        [JsonPropertyName("mfaRequired")]
        public required bool MfaRequired { get; set; } = false;

        [JsonPropertyName("mfaSessionToken")]
        public string? MfaSessionToken { get; set; }
    }
}
