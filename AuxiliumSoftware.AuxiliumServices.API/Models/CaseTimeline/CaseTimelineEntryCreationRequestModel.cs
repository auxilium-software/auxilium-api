using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AuxiliumSoftware.AuxiliumServices.API.Models.CaseTimeline
{
    public class CaseTimelineEntryCreationRequestModel
    {
        [Required]
        [JsonPropertyName("occurredAtUtc")]
        public required DateTime OccurredAtUtc { get; set; }



        [Required]
        [JsonPropertyName("title")]
        public required string Title { get; set; } = string.Empty;



        [JsonPropertyName("description")]
        public string? Description { get; set; }
    }
}
