using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AuxiliumSoftware.AuxiliumServices.API.Models.CalendarEvent
{
    public class CalendarEventInviteCreateRequestModel
    {
        [Required]
        [JsonPropertyName("userIds")]
        public required List<Guid> UserIds { get; init; }
    }
}
