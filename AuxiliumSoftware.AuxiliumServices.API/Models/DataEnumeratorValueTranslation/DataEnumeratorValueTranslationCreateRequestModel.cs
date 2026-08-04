using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AuxiliumSoftware.AuxiliumServices.API.Models.DataEnumeratorValueTranslation
{
    public class DataEnumeratorValueTranslationCreateRequestModel
    {
        [Required]
        [JsonPropertyName("languageCode")]
        public required string LanguageCode { get; set; }



        [Required]
        [JsonPropertyName("translation")]
        public required string Translation { get; set; }
    }
}
