namespace AuxiliumSoftware.AuxiliumServices.API.Models.MyTotp
{
    public class TotpEnableResponseModel
    {
        public bool IsEnabled { get; set; }

        /// <summary>
        /// One-time-use recovery codes.
        /// These are shown ONCE and cannot be retrieved again.
        /// </summary>
        public List<string> RecoveryCodes { get; set; } = new();
    }
}
