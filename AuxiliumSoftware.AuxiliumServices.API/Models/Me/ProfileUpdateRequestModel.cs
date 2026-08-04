using System.Text.Json.Serialization;

namespace AuxiliumSoftware.AuxiliumServices.API.Models.Me
{
    public class ProfileUpdateRequestModel
    {
        [JsonPropertyName("fullName")]
        public string? FullName { get; set; }


        [JsonPropertyName("fullAddress")]
        public string? FullAddress { get; set; }


        [JsonPropertyName("telephoneNumber")]
        public string? TelephoneNumber { get; set; }


        [JsonPropertyName("gender")]
        public string? Gender { get; set; }


        [JsonPropertyName("dateOfBirth")]
        public DateOnly? DateOfBirth { get; set; }


        [JsonPropertyName("howDidYouFindOutAboutOurService")]
        public string? HowDidYouFindOutAboutOurService { get; set; }


        [JsonPropertyName("languagePreference")]
        public string? LanguagePreference { get; set; }
    }
}
