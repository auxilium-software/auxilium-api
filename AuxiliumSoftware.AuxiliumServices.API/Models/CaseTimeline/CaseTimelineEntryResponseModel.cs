using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AuxiliumSoftware.AuxiliumServices.API.Models.CaseTimeline
{
    public class CaseTimelineEntryResponseModel
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }



        [JsonPropertyName("caseId")]
        public Guid CaseId { get; set; }



        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }



        [JsonPropertyName("createdBy")]
        public Guid? CreatedBy { get; set; }



        [JsonPropertyName("occurredAtUtc")]
        public DateTime OccurredAtUtc { get; set; }



        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;



        [JsonPropertyName("description")]
        public string? Description { get; set; }
    }
}
