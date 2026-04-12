using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework.Enumerators;
using System.Net;

namespace AuxiliumSoftware.AuxiliumServices.API.Models.Waf.ResponseModels.SubResponseModels
{
    public class LoginAttemptItem
    {
        public required Guid Id { get; set; }
        public required DateTime AttemptedAt { get; set; }
        public required IPAddress? IpAddress { get; set; }
        public required string? TargetEmail { get; set; }
        public Guid? TargetUserId { get; set; } = null;
        public required bool WasSuccessful { get; set; }
        public required bool WasBlockedByWaf { get; set; }
        public required LoginAttemptFailureReasonEnum? FailureReason { get; set; }
    }
}
