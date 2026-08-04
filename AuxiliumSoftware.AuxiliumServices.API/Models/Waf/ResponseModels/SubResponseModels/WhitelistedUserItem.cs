namespace AuxiliumSoftware.AuxiliumServices.API.Models.Waf.ResponseModels.SubResponseModels
{
    public class WhitelistedUserItem
    {
        public required Guid UserId { get; set; }
        public required string? Reason { get; set; }
        public required bool IsPermanent { get; set; }
        public required bool IsActive { get; set; }
        public required DateTime WhitelistedAt { get; set; }
        public required Guid? WhitelistedByUserId { get; set; }
        public required DateTime? ExpiresAt { get; set; }
        public DateTime? RemovedAt { get; set; } = null;
        public Guid? RemovedByUserId { get; set; } = null;
        public string? RemovalReason { get; set; } = null;
    }
}
