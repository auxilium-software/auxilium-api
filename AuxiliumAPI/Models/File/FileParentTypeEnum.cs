using System.Text.Json.Serialization;

namespace AuxiliumAPI.Models.File
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
