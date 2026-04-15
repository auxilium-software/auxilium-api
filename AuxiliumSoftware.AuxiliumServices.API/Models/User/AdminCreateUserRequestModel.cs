using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AuxiliumSoftware.AuxiliumServices.API.Models.User
{
    public class AdminCreateUserRequestModel
    {
        [Required]
        [EmailAddress]
        [JsonPropertyName("emailAddress")]
        public required string EmailAddress { get; set; }


        [Required]
        [JsonPropertyName("fullName")]
        public string FullName { get; set; } = string.Empty;


        [Required]
        [JsonPropertyName("languagePreference")]
        public string? LanguagePreference { get; set; }
    }
}
