using System.Text.Json.Serialization;

namespace AuxiliumAPI.Models.Case
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum CaseStatusEnum
    {
        [JsonPropertyName("open")]
        Open,

        [JsonPropertyName("in_progress")]
        InProgress,

        [JsonPropertyName("pending")]
        Pending,

        [JsonPropertyName("closed")]
        Closed,

        [JsonPropertyName("archived")]
        Archived
    }
}
