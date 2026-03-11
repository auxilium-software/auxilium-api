namespace AuxiliumSoftware.AuxiliumServices.API.Models.Waf.ResponseModels.SubResponseModels
{
    public class TopOffenderItem
    {
        public required string IpAddress { get; set; }
        public required int FailedAttempts { get; set; }
        public required int DistinctUsersTargeted { get; set; }
        public required DateTime LastAttempt { get; set; }
        public required bool IsCurrentlyBlocked { get; set; }
    }
}
