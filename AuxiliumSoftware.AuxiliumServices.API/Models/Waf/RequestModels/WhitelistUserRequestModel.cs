namespace AuxiliumSoftware.AuxiliumServices.API.Models.Waf.RequestModels
{
    public class WhitelistUserRequestModel
    {
        public required string Reason { get; set; }
        public required bool IsPermanent { get; set; }
    }
}
