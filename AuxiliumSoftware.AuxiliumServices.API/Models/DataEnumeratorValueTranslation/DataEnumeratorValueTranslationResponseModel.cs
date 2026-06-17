using System.Text.Json.Serialization;

namespace AuxiliumSoftware.AuxiliumServices.API.Models.DataEnumeratorValueTranslation
{
    public class DataEnumeratorValueTranslationResponseModel
    {
        [JsonPropertyName("id")]
        public required Guid Id { get; set; }



        [JsonPropertyName("dataEnumeratorValueId")]
        public required Guid DataEnumeratorValueId { get; set; }



        [JsonPropertyName("languageCode")]
        public required string LanguageCode { get; set; }



        [JsonPropertyName("translation")]
        public required string Translation { get; set; }



        [JsonPropertyName("createdAtUtc")]
        public required DateTime CreatedAtUtc { get; set; }



        [JsonPropertyName("lastUpdatedAtUtc")]
        public DateTime? LastUpdatedAtUtc { get; set; }
    }
}
