using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AuxiliumSoftware.AuxiliumServices.API.Models.Me
{
    public class RequestMyAccountDeletionRequestModel
    {
        [Required]
        [JsonPropertyName("currentPasswordSha512")]
        public required string CurrentPasswordSha512 { get; init; }



        [Required]
        [JsonPropertyName("reason")]
        public required string? Reason { get; init; } = null;
    }
}
