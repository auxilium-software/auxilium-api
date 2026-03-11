namespace AuxiliumSoftware.AuxiliumServices.API.Models.Waf.ResponseModels.SubResponseModels
{
    public class TopTargetedUserItem
    {
        public required Guid UserId { get; set; }
        public required int FailedAttempts { get; set; }
        public required int DistinctIpAddresses { get; set; }
        public required DateTime LastAttempt { get; set; }
        public bool IsCurrentlyLockedOut { get; set; }
    }
}
