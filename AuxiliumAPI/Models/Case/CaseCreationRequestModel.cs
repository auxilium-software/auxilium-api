using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AuxiliumAPI.Models.Case
{
    public class CaseCreationRequestModel
    {
        [Required]
        [JsonPropertyName("title")]
        public required string Title { get; init; }

        [Required]
        [JsonPropertyName("description")]
        public required string Description { get; init; }
    }
}
