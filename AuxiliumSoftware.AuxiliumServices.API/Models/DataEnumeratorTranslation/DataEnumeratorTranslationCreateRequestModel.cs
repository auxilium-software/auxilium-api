using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AuxiliumSoftware.AuxiliumServices.API.Models.DataEnumeratorTranslation
{
    public class DataEnumeratorTranslationCreateRequestModel
    {
        [Required]
        [JsonPropertyName("languageCode")]
        public required string LanguageCode { get; set; }



        [Required]
        [JsonPropertyName("translation")]
        public required string Translation { get; set; }
    }
}
