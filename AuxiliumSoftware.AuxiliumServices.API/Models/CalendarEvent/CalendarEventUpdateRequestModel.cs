using System.Text.Json.Serialization;

namespace AuxiliumSoftware.AuxiliumServices.API.Models.CalendarEvent
{
    public class CalendarEventUpdateRequestModel
    {
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("start")]
        public DateTime? Start { get; set; }

        [JsonPropertyName("end")]
        public DateTime? End { get; set; }

        [JsonPropertyName("allDay")]
        public bool? AllDay { get; set; }

        [JsonPropertyName("categoryId")]
        public Guid? CategoryId { get; set; }

        [JsonPropertyName("caseId")]
        public Guid? CaseId { get; set; }

        [JsonPropertyName("location")]
        public string? Location { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }
    }
}
