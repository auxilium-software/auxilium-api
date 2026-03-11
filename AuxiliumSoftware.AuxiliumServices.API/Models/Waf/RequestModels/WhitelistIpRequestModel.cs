using System.Net;

namespace AuxiliumSoftware.AuxiliumServices.API.Models.Waf.RequestModels
{
    public class WhitelistIpRequestModel
    {
        public required IPAddress IpAddress { get; set; }
        public required string Reason { get; set; }
        public required bool IsPermanent { get; set; }
    }
}
