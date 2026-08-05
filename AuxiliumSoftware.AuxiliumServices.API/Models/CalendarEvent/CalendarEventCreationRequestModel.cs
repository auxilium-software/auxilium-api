using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AuxiliumSoftware.AuxiliumServices.API.Models.CalendarEvent
{
    public class CalendarEventCreationRequestModel
    {
        [Required]
        [JsonPropertyName("title")]
        public required string Title { get; init; }

        [Required]
        [JsonPropertyName("start")]
        public required DateTime Start { get; init; }

        [Required]
        [JsonPropertyName("end")]
        public required DateTime End { get; init; }

        [JsonPropertyName("allDay")]
        public bool AllDay { get; init; }

        [Required]
        [JsonPropertyName("categoryId")]
        public required Guid CategoryId { get; init; }

        [JsonPropertyName("caseId")]
        public Guid? CaseId { get; init; }

        [JsonPropertyName("location")]
        public string? Location { get; init; }

        [JsonPropertyName("description")]
        public string? Description { get; init; }

        [JsonPropertyName("inviteeUserIds")]
        public List<Guid>? InviteeUserIds { get; init; }
    }
}
