using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework.Enumerators;

namespace AuxiliumSoftware.AuxiliumServices.API.Models.SystemBulletin
{
    public class SystemBulletinResponseModel
    {
        public Guid Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public SystemBulletinMessageSeverityEnum Severity { get; set; }
        public required string Title { get; set; }
        public required string Content { get; set; }
        public bool IsDismissible { get; set; }
    }
}
