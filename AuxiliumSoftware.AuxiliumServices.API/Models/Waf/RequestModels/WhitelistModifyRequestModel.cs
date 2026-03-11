using System.Net;

namespace AuxiliumSoftware.AuxiliumServices.API.Models.Waf.RequestModels
{
    public class WhitelistModifyRequestModel
    {
        public required IPAddress IpAddress { get; set; }
        public required string? Reason { get; set; }
    }
}
