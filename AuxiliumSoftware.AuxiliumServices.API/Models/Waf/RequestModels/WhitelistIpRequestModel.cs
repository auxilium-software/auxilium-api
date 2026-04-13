using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text.Json.Serialization;

namespace AuxiliumSoftware.AuxiliumServices.API.Models.Waf.RequestModels
{
    public class WhitelistIpRequestModel
    {
        [Required]
        [JsonPropertyName("ipAddress")]
        public required string IpAddress { get; set; }

        [Required]
        [JsonPropertyName("reason")]
        public required string Reason { get; set; }

        [Required]
        [JsonPropertyName("isPermanent")]
        public required bool IsPermanent { get; set; }
    }
}
