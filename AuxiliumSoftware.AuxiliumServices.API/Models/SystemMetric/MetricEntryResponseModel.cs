using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AuxiliumSoftware.AuxiliumServices.API.Models.SystemMetric
{
    public class MetricEntryResponseModel
    {
        [Key]
        [Required]
        [JsonPropertyName("id")]
        public required Guid Id { get; set; }

        [Required]
        [JsonPropertyName("createdAt")]
        public required DateTime CreatedAt { get; set; }

        [Required]
        [JsonPropertyName("value")]
        public required double MetricValue { get; set; }
    }
}
