namespace AuxiliumSoftware.AuxiliumServices.API.Models.MyTotp
{
    public class TotpVerifyRequestModel
    {
        /// <summary>
        /// The 6-digit TOTP code from the authenticator app.
        /// </summary>
        public string Code { get; set; } = string.Empty;
    }
}
