using AuxiliumSoftware.AuxiliumServices.API.Models.DataEnumeratorValue;
using System.Text.Json.Serialization;

namespace AuxiliumSoftware.AuxiliumServices.API.Models.DataEnumerator
{
    public class DataEnumeratorResponseModel
    {
        [JsonPropertyName("id")]
        public required Guid Id { get; set; }



        [JsonPropertyName("canonicalName")]
        public required string CanonicalName { get; set; }



        [JsonPropertyName("description")]
        public string? Description { get; set; }



        [JsonPropertyName("isActive")]
        public required bool IsActive { get; set; }



        [JsonPropertyName("createdAtUtc")]
        public required DateTime CreatedAtUtc { get; set; }



        [JsonPropertyName("lastUpdatedAtUtc")]
        public DateTime? LastUpdatedAtUtc { get; set; }



        [JsonPropertyName("values")]
        public List<DataEnumeratorValueResponseModel>? Values { get; set; }
    }
}
