using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AuxiliumSoftware.AuxiliumServices.API.Models
{
    /// <summary>
    /// Represents a standard failure response model.
    /// </summary>
    public class FailureResponseModel
    {
        /**
         * The status of the response, always set to "failure".
         */
        [Required]
        [JsonPropertyName("status")]
        public string Status { get; } = "failure";


        /**
         * Any details about the failure
         */
        [Required]
        [JsonPropertyName("detail")]
        public required string Detail { get; init; }
    }
}
