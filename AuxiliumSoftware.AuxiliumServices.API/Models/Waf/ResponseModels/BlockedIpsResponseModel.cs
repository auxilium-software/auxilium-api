using AuxiliumSoftware.AuxiliumServices.API.Models.Waf.ResponseModels.SubResponseModels;

namespace AuxiliumSoftware.AuxiliumServices.API.Models.Waf.ResponseModels
{
    public class BlockedIpsResponseModel
    {
        public required List<BlockedIpItem> Blocks { get; set; }
        public required int TotalCount { get; set; }
        public required int Page { get; set; }
        public required int PageSize { get; set; }
        public required int TotalPages { get; set; }
    }
}
