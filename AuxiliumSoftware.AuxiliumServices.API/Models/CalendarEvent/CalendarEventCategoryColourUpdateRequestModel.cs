using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AuxiliumSoftware.AuxiliumServices.API.Models.CalendarEventCategory
{
    public class CalendarEventCategoryColourUpdateRequestModel
    {
        [Required]
        [JsonPropertyName("colour")]
        public required string Colour { get; init; }
    }
}
