using System.Text.Json.Serialization;

namespace AuxiliumSoftware.AuxiliumServices.API.Models.DataEnumerator
{
    public class DataEnumeratorUpdateRequestModel
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }



        [JsonPropertyName("description")]
        public string? Description { get; set; }
    }
}
