using System.Net;

namespace AuxiliumSoftware.AuxiliumServices.API.Models.Waf.ResponseModels.SubResponseModels
{
    public class TopOffenderItem
    {
        public required IPAddress IpAddress { get; set; }
        public required int FailedAttempts { get; set; }
        public required int DistinctUsersTargeted { get; set; }
        public required DateTime LastAttempt { get; set; }
        public bool IsCurrentlyBlocked { get; set; }
    }
}
