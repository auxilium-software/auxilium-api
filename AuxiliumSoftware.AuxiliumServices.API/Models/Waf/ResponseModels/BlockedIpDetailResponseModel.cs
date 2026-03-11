using AuxiliumSoftware.AuxiliumServices.API.Models.Waf.ResponseModels.SubResponseModels;
using System.Net;

namespace AuxiliumSoftware.AuxiliumServices.API.Models.Waf.ResponseModels
{
    public class BlockedIpDetailResponseModel
    {
        public required IPAddress IpAddress { get; set; }
        public required BlockedIpItem? CurrentBlock { get; set; }
        public required List<LoginAttemptItem> RecentLoginAttempts { get; set; }
        public required List<BlockHistoryItem> BlockHistory { get; set; }
        public required int TotalFailedAttempts { get; set; }
        public required int TotalBlockedAttempts { get; set; }
        public required DateTime? FirstSeenAt { get; set; }
        public required DateTime? LastSeenAt { get; set; }
    }
}
