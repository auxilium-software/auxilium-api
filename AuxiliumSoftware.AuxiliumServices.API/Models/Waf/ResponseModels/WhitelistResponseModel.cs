using System.Net;

namespace AuxiliumSoftware.AuxiliumServices.API.Models.Waf.ResponseModels
{
    public class WhitelistResponseModel
    {
        public required List<IPAddress> IpAddresses { get; set; }
        public required int Count { get; set; }
    }
}
