using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework.Enumerators;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AuxiliumSoftware.AuxiliumServices.API.Models.CalendarEvent
{
    public class CalendarEventInviteRespondRequestModel
    {
        [Required]
        [JsonPropertyName("status")]
        public required CalendarEventInviteStatusEnum Status { get; init; }
    }
}
