using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AuxiliumSoftware.AuxiliumServices.API.Models.AdditionalProperty
{
    public class AdditionalPropertyUpdateRequestModel
    {
        [Required]
        [JsonPropertyName("content")]
        public required string Content { get; set; }


        [Required]
        [JsonPropertyName("contentType")]
        public string? ContentType { get; set; }
    }
}
