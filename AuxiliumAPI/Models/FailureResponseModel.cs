using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AuxiliumAPI.Models
{
    public class FailureResponseModel
    {
        [Required]
        [JsonPropertyName("status")]
        public string Status { get; } = "failure";


        [Required]
        [JsonPropertyName("detail")]
        public required string Detail { get; init; }
    }
}
