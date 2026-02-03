using System.ComponentModel.DataAnnotations;

namespace AuxiliumSoftware.AuxiliumServices.API.Models.UserLogin
{
    public class VerifyRecoveryCodeRequestModel
    {
        [Required]
        public string MfaSessionToken { get; set; } = string.Empty;

        [Required]
        public string RecoveryCode { get; set; } = string.Empty;
    }
}
