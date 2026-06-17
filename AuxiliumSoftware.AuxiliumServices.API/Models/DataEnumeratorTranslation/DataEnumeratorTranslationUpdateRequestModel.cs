using System.Text.Json.Serialization;

namespace AuxiliumSoftware.AuxiliumServices.API.Models.DataEnumeratorTranslation
{
    public class DataEnumeratorTranslationUpdateRequestModel
    {
        [JsonPropertyName("languageCode")]
        public string? LanguageCode { get; set; }



        [JsonPropertyName("translation")]
        public string? Translation { get; set; }
    }
}
