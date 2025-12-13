using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AuxiliumAPI.Models
{
    public class SuccessResponseModel
    {
        [Required]
        [JsonPropertyName("status")]
        public string Status { get; } = "success";
    }
}
