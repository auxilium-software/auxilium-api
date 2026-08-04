using System.Net;

namespace AuxiliumSoftware.AuxiliumServices.API.Models.Waf.ResponseModels.SubResponseModels
{
    public class BlacklistIpAddressRequestModel
    {
        public required IPAddress IpAddress { get; set; }
        public required string Reason { get; set; }
        public required bool IsPermanent { get; set; }
        public required int? DurationMinutes { get; set; } = 60;
    }
}
