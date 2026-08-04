using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework.Enumerators;

namespace AuxiliumSoftware.AuxiliumServices.API.Models.SystemSettings
{
    public class SystemSettingResponseModel
    {
        public required SystemSettingKeyEnum Key { get; set; }
        public required string Value { get; set; }
        public required SystemSettingValueTypeEnum ValueType { get; set; }
        public required DateTime ModifiedAt { get; set; }
        public Guid? ModifiedBy { get; set; }
        public string? ModifiedByName { get; set; }
        public required string ReasonForModification { get; set; }
    }
}
