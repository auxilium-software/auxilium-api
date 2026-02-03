using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework.Enumerators;

namespace AuxiliumSoftware.AuxiliumServices.API.Models.SystemBulletin
{
    public class SystemBulletinCreationRequestModel
    {
        public SystemBulletinMessageSeverityEnum Severity { get; set; } = SystemBulletinMessageSeverityEnum.Informational;
        public required string Title { get; set; }
        public required string Content { get; set; }
        public bool IsDismissible { get; set; } = true;
        public DateTime? StartsAt { get; set; }
        public DateTime? EndsAt { get; set; }
        public SystemBulletinMessageTargetAudienceEnum TargetAudience { get; set; } = SystemBulletinMessageTargetAudienceEnum.Everyone;
        public Guid? SpecificUserId { get; set; }
    }
}
