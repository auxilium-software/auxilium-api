using System.Text.Json.Serialization;

namespace AuxiliumSoftware.AuxiliumServices.API.Models.File
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum FileParentTypeEnum
    {
        [JsonPropertyName("case")]
        Case,

        [JsonPropertyName("user")]
        User
    }
}
