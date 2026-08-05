using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AuxiliumSoftware.AuxiliumServices.API.Models.CalendarEventCategory
{
    public class CalendarEventCategoryResponseModel
    {
        [Required]
        [JsonPropertyName("id")]
        public required Guid Id { get; init; }

        [Required]
        [JsonPropertyName("name")]
        public required string Name { get; init; }

        [Required]
        [JsonPropertyName("isActive")]
        public required bool IsActive { get; init; }

        [Required]
        [JsonPropertyName("sortOrder")]
        public required int SortOrder { get; init; }

        [Required]
        [JsonPropertyName("colour")]
        public required string Colour { get; init; }
    }
}
