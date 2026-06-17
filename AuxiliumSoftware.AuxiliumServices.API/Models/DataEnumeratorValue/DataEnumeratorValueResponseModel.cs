using System.Text.Json.Serialization;

namespace AuxiliumSoftware.AuxiliumServices.API.Models.DataEnumeratorValue
{
    public class DataEnumeratorValueResponseModel
    {

        [JsonPropertyName("id")]
        public required Guid Id { get; set; }



        [JsonPropertyName("enumTypeId")]
        public required Guid EnumTypeId { get; set; }



        [JsonPropertyName("canonicalName")]
        public required string CanonicalName { get; set; }



        [JsonPropertyName("isActive")]

        public required bool IsActive { get; set; }



        [JsonPropertyName("sortOrder")]
        public required int SortOrder { get; set; }



        [JsonPropertyName("createdAtUtc")]
        public required DateTime CreatedAtUtc { get; set; }



        [JsonPropertyName("lastUpdatedAtUtc")]
        public DateTime? LastUpdatedAtUtc { get; set; }
    }
}
