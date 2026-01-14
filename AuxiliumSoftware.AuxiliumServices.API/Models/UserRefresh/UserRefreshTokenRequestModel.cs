using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AuxiliumSoftware.AuxiliumServices.API.Models.UserRefresh
{
    public class UserRefreshTokenRequestModel
    {
        [Required]
        [JsonPropertyName("refreshToken")]
        public required string RefreshToken { get; init; }
    }
}
