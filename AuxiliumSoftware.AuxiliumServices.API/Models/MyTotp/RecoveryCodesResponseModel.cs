namespace AuxiliumSoftware.AuxiliumServices.API.Models.MyTotp
{
    public class RecoveryCodesResponseModel
    {
        /// <summary>
        /// Newly generated list of one-time-use recovery codes.
        /// The old codes MUST be destroyed when new ones are generated.
        /// </summary>
        public List<string> RecoveryCodes { get; set; } = new();
    }
}
