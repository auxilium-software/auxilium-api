using System.Text.Json.Serialization;

namespace AuxiliumAPI.Models.Case
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum TodoStatusEnum
    {
        [JsonPropertyName("needs_action")]
        NeedsAction,

        [JsonPropertyName("in_progress")]
        InProgress,

        [JsonPropertyName("completed")]
        Completed,

        [JsonPropertyName("cancelled")]
        Cancelled,
    }
}
