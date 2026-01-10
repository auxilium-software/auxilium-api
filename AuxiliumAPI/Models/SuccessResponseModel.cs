using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AuxiliumAPI.Models
{
    /// <summary>
    /// Represents a standard success response model.
    /// </summary>
    public class SuccessResponseModel
    {
        /**
         * The status of the response, always set to "success".
         */
        [Required]
        [JsonPropertyName("status")]
        public string Status { get; } = "success";
    }
}
