using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AuxiliumAPI.Models.UserLogin
{
    public class UserLoginResponseModel
    {
        [Required]
        [JsonPropertyName("accessToken")]
        public required string AccessToken { get; init; }

        [Required]
        [JsonPropertyName("refreshToken")]
        public required string RefreshToken { get; init; }

        [Required]
        [JsonPropertyName("tokenType")]
        public string TokenType { get; init; } = "Bearer";

        [Required]
        [JsonPropertyName("expiresIn")]
        public required int ExpiresIn { get; init; }
    }
}
