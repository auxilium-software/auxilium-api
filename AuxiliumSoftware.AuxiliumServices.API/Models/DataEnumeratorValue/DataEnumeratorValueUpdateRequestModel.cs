using System.Text.Json.Serialization;

namespace AuxiliumSoftware.AuxiliumServices.API.Models.DataEnumeratorValue
{
    public class DataEnumeratorValueUpdateRequestModel
    {
        [JsonPropertyName("displayName")]
        public string? DisplayName { get; set; }



        [JsonPropertyName("colourHex")]
        public string? ColourHex { get; set; }



        [JsonPropertyName("sortOrder")]
        public int? SortOrder { get; set; }
    }
}
