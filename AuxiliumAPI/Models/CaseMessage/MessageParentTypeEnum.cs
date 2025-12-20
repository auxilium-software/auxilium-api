using System.Text.Json.Serialization;

namespace AuxiliumAPI.Models.File
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum MessageParentTypeEnum
    {
        [JsonPropertyName("case")]
        Case
    }
}
