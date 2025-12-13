using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AuxiliumAPI.Models.Case
{
    public class AddPersonRequestModel
    {
        [Required]
        [JsonPropertyName("userId")]
        public required string UserID { get; init; }
    }
}
