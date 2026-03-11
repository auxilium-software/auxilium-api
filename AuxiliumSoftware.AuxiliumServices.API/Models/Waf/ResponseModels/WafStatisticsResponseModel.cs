using AuxiliumSoftware.AuxiliumServices.API.Models.Waf.ResponseModels.SubResponseModels;

namespace AuxiliumSoftware.AuxiliumServices.API.Models.Waf.ResponseModels
{
    public class WafStatisticsResponseModel
    {
        public required bool WafEnabled { get; set; }
        public required DateTime PeriodStart { get; set; }
        public required DateTime PeriodEnd { get; set; }

        // counters
        public required int TotalLoginAttempts { get; set; }
        public required int SuccessfulLoginAttempts { get; set; }
        public required int FailedLoginAttempts { get; set; }
        public required int BlockedByWafAttempts { get; set; }

        // current state
        public required int CurrentlyBlockedIps { get; set; }
        public required int PermanentlyBannedIps { get; set; }
        public required int TemporarilyBlockedIps { get; set; }
        public required int LockedOutUsers { get; set; }

        // distinct counts
        public required int DistinctOffendingIps { get; set; }
        public required int DistinctTargetedUsers { get; set; }

        // chart data
        public required List<HourlyBreakdownItem> HourlyBreakdown { get; set; }

        // top lists
        public required List<TopOffenderItem> TopOffendingIps { get; set; }
        public required List<TopTargetedUserItem> TopTargetedUsers { get; set; }
    }
}
