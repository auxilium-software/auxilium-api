using System.Text.Json.Serialization;

namespace AuxiliumSoftware.AuxiliumServices.API.Models.CaseMessage
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum MessageParentTypeEnum
    {
        [JsonPropertyName("case")]
        Case
    }
}
