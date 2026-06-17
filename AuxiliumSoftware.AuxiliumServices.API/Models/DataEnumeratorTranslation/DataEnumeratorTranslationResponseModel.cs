using System.Text.Json.Serialization;

namespace AuxiliumSoftware.AuxiliumServices.API.Models.DataEnumeratorTranslation
{
    public class DataEnumeratorTranslationResponseModel
    {
        [JsonPropertyName("id")]
        public required Guid Id { get; set; }



        [JsonPropertyName("dataEnumeratorId")]
        public required Guid DataEnumeratorId { get; set; }



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
