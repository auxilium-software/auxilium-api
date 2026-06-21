using System.Text.Json.Serialization;

namespace AuxiliumSoftware.AuxiliumServices.API.Models.CaseTimeline
{
    public class CaseTimelineEntryUpdateRequestModel
    {
        [JsonPropertyName("occurredAtUtc")]
        public DateTime? OccurredAtUtc { get; set; }



        [JsonPropertyName("title")]
        public string? Title { get; set; }



        [JsonPropertyName("description")]
        public string? Description { get; set; }
    }
}
