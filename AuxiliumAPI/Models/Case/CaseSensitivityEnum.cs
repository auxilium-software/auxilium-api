using System.Text.Json.Serialization;

namespace AuxiliumAPI.Models.Case
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum CaseSensitivityEnum
    {
        [JsonPropertyName("public")]
        Public,

        [JsonPropertyName("internal")]
        Internal,

        [JsonPropertyName("confidential")]
        Confidential,

        [JsonPropertyName("restricted")]
        Restricted
    }
}
