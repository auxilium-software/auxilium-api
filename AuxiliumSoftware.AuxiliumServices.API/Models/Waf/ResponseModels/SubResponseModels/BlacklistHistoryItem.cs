namespace AuxiliumSoftware.AuxiliumServices.API.Models.Waf.ResponseModels.SubResponseModels
{
    public class BlacklistHistoryItem
    {
        public required Guid Id { get; set; }
        public required DateTime BlockedAt { get; set; }
        public required DateTime? ExpiresAt { get; set; }
        public required bool IsPermanent { get; set; }
        public required string? Reason { get; set; }
        public required bool WasManuallyUnblocked { get; set; }
        public required DateTime? UnblockedAt { get; set; }
        public required string? UnblockReason { get; set; }
    }
}
