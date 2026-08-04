namespace AuxiliumSoftware.AuxiliumServices.API.Models.SystemSettings
{
    public class SystemSettingUpdateRequestModel
    {
        public required object Value { get; set; }
        public required string ReasonForModification { get; set; }
    }
}
