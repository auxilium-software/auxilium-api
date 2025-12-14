using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AuxiliumAPI.Models.UserRegistration
{
    public class UserRegistrationResponseModel
    {
        [Required]
        [JsonPropertyName("id")]
        public required Guid Id { get; init; }


        [Required]
        [EmailAddress]
        [JsonPropertyName("emailAddress")]
        public required string EmailAddress { get; init; }
    }
}
