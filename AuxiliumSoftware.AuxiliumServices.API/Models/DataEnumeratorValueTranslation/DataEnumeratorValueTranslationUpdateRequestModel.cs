using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AuxiliumSoftware.AuxiliumServices.API.Models.DataEnumeratorValueTranslation
{
    public class DataEnumeratorValueTranslationUpdateRequestModel
    {
        [Required]
        [JsonPropertyName("languageCode")]
        public string? LanguageCode { get; set; }



        [JsonPropertyName("translation")]
        public string? Translation { get; set; }
    }
}
