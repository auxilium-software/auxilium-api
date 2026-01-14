using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AuxiliumSoftware.AuxiliumServices.API.Models.Case
{
    public class AddPersonRequestModel
    {
        [Required]
        [JsonPropertyName("userId")]
        public required Guid UserID { get; init; }
    }
}
