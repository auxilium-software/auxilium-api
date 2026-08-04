using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework.Enumerators;

namespace AuxiliumSoftware.AuxiliumServices.API.Models.SystemBulletin
{
    public class SystemBulletinCreationRequestModel
    {
        public required SystemBulletinMessageSeverityEnum Severity { get; set; } = SystemBulletinMessageSeverityEnum.Informational;
        public required string Title { get; set; }
        public required string Content { get; set; }
        public required bool IsDismissible { get; set; } = true;
        public required DateTime? StartsAt { get; set; }
        public required DateTime? EndsAt { get; set; }
        public required SystemBulletinMessageTargetAudienceEnum TargetAudience { get; set; } = SystemBulletinMessageTargetAudienceEnum.Everyone;
        public required Guid? SpecificUserId { get; set; }
    }
}
