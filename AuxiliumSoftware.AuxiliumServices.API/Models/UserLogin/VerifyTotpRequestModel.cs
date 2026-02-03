using System.ComponentModel.DataAnnotations;

namespace AuxiliumSoftware.AuxiliumServices.API.Models.UserLogin
{
    public class VerifyTotpRequestModel
    {
        [Required]
        public string MfaSessionToken { get; set; } = string.Empty;


        [Required]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "TOTP code must be 6 digits")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "TOTP code must be 6 digits")]
        public string TotpCode { get; set; } = string.Empty;
    }
}
