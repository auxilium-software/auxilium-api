using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AuxiliumSoftware.AuxiliumServices.API.Models.UserCreation
{
    public class CreateUserRequestModel
    {

        [Required]
        [JsonPropertyName("fullName")]
        public required string FullName { get; set; } = string.Empty;

        [Required]
        [JsonPropertyName("emailAddress")]
        public required string EmailAddress { get; set; } = string.Empty;

        [JsonPropertyName("telephoneNumber")]
        public string? TelephoneNumber { get; set; }

        [JsonPropertyName("dateOfBirth")]
        public DateTime? DateOfBirth { get; set; }

        [Required]
        [JsonPropertyName("languagePreference")]
        public required string LanguagePreference { get; set; } = "en-GB";
    }
}
