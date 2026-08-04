using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework.Enumerators;

namespace AuxiliumSoftware.AuxiliumServices.API.Models.SystemBulletin
{
    public class SystemBulletinAdminResponseModel
    {
        public required Guid Id { get; set; }
        public required DateTime CreatedAt { get; set; }
        public required Guid? CreatedBy { get; set; }
        public required SystemBulletinMessageSeverityEnum Severity { get; set; }
        public required string Title { get; set; }
        public required string Content { get; set; }
        public required bool IsActive { get; set; }
        public required bool IsDismissible { get; set; }
        public required DateTime StartsAt { get; set; }
        public required DateTime? EndsAt { get; set; }
        public required SystemBulletinMessageTargetAudienceEnum TargetAudience { get; set; }
        public required Guid? SpecificUserId { get; set; }
    }
}
