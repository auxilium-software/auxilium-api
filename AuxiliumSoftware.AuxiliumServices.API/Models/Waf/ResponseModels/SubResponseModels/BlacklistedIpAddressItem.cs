namespace AuxiliumSoftware.AuxiliumServices.API.Models.Waf.ResponseModels.SubResponseModels
{
    public class BlacklistedIpAddressItem
    {
        public required Guid Id { get; set; }
        public required string IpAddress { get; set; }
        public required string Justification { get; set; }
        public required bool IsPermanent { get; set; }
        public required bool IsActive { get; set; }
        public required DateTime BlockedAt { get; set; }
        public DateTime? ExpiresAt { get; set; } = null;
        public DateTime? UnblockedAt { get; set; } = null;
        public Guid? UnblockedByUserId { get; set; } = null;
        public string? UnblockJustification { get; set; } = null;
    }
}
