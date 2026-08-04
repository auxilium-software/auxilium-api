using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework.Enumerators;
using AuxiliumSoftware.AuxiliumServices.Common.Metrics.Enumerators;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AuxiliumSoftware.AuxiliumServices.API.Models.SystemMetric
{
    public class MetricsOverTimeResponseModel
    {
        [Required]
        [JsonPropertyName("metricKey")]
        public required SystemMetricKeyEnum MetricKey { get; set; }

        [Required]
        [JsonPropertyName("metrics")]
        public required List<MetricEntryResponseModel> Metrics { get; set; }
    }
}
