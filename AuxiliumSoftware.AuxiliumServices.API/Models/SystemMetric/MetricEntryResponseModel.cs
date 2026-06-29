using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AuxiliumSoftware.AuxiliumServices.API.Models.SystemMetric
{
    public class MetricEntryResponseModel
    {
        [Required]
        [JsonPropertyName("createdAt")]
        public required DateTime CreatedAt { get; set; }

        [Required]
        [JsonPropertyName("value")]
        public required double MetricValue { get; set; }
    }
}
