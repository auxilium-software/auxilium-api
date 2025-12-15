using System.Text.Json.Serialization;

namespace AuxiliumAPI.Models.Server
{
    public class PingResponseModel
    {
        [JsonPropertyName("response")]
        public required string Response { get; set; } = "pong";
    }
}
