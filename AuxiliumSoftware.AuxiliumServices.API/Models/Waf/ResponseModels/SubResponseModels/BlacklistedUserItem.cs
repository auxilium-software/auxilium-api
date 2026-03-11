namespace AuxiliumSoftware.AuxiliumServices.API.Models.Waf.ResponseModels.SubResponseModels
{
    public class BlacklistedUserItem
    {
        public required Guid UserId { get; set; }
        public required bool IsLockedOut { get; set; }
        public required DateTime? LockedOutAt { get; set; } = null;
        public required DateTime? LockoutEndsAt { get; set; } = null;
        public required string? LockoutReason { get; set; } = null;
        public int RecentFailedAttempts24h { get; set; }
        public int DistinctIpAddresses24h { get; set; }
    }
}
