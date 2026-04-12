using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AuxiliumSoftware.AuxiliumServices.API.Models.UserRegistration
{
    public class UserRegistrationRequestModel
    {
        [Required]
        [JsonPropertyName("recaptchaToken")]
        public required string RecaptchaToken { get; init; }


        [Required]
        [JsonPropertyName("onBehalfOf")]
        public required string OnBehalfOf { get; init; }

        [Required]
        [JsonPropertyName("dataProcessingConsent")]
        public required string DataProcessingConsent { get; init; }

        [Required]
        [JsonPropertyName("fullName")]
        public required string FullName { get; init; }

        [Required]
        [JsonPropertyName("telephoneNumber")]
        public required string TelephoneNumber { get; init; }

        [Required]
        [JsonPropertyName("fullAddress")]
        public required string FullAddress { get; init; }

        [Required]
        [JsonPropertyName("gender")]
        public required string Gender { get; init; }

        [Required]
        [JsonPropertyName("ethnicGroup")]
        public required string EthnicGroup { get; init; }

        [Required]
        [JsonPropertyName("dateOfBirth")]
        public required string DateOfBirth { get; init; }

        [Required]
        [JsonPropertyName("howDidYouFindOutAboutOurService")]
        public required string HowDidYouFindOutAboutOurService { get; init; }

        [Required]
        [JsonPropertyName("languagePreference")]
        public required string LanguagePreference { get; init; }

        [Required]
        [EmailAddress]
        [JsonPropertyName("emailAddress")]
        public required string EmailAddress { get; init; }

        [JsonPropertyName("rawPassword")]
        public string? RawPassword { get; init; }
        
        [JsonPropertyName("passwordSha512")]
        public string? PasswordSha512 { get; set; }

        [Required]
        [JsonPropertyName("caseTitle")]
        public required string CaseTitle { get; init; }

        [Required]
        [JsonPropertyName("caseDescription")]
        public required string CaseDescription { get; init; }
    }
}
