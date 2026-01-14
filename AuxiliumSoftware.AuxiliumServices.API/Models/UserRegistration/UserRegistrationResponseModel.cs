using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AuxiliumSoftware.AuxiliumServices.API.Models.UserRegistration
{
    public class UserRegistrationResponseModel
    {
        [Required]
        [JsonPropertyName("id")]
        public required Guid Id { get; init; }
    }
}
