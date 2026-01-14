using System.Text.Json.Serialization;

namespace AuxiliumSoftware.AuxiliumServices.API.Models.Server
{
    public class PingResponseModel
    {
        [JsonPropertyName("response")]
        public required string Response { get; set; } = "pong";
    }
}
