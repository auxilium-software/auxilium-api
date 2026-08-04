namespace AuxiliumSoftware.AuxiliumServices.API.Models.MyTotp
{
    public class TotpSetupResponseModel
    {
        /// <summary>
        /// Base32-encoded secret for manual entry.
        /// Only returned once during setup.
        /// </summary>
        public string Secret { get; set; } = string.Empty;

        /// <summary>
        /// "otpauth://" URI for QR code generation.
        /// </summary>
        public string ProvisioningUri { get; set; } = string.Empty;
    }
}
