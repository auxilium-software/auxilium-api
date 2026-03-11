namespace AuxiliumSoftware.AuxiliumServices.API.Models.SystemSettings
{
    public class SystemSettingItemResponseModel
    {
        public string Key { get; set; } = string.Empty;
        public string JsonKey { get; set; } = string.Empty;
        public object? Value { get; set; }
        public string ValueType { get; set; } = string.Empty;
        public object? DefaultValue { get; set; }
        public bool IsUsingDefault { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Recommendation { get; set; } = string.Empty;
        public DateTime? LastModifiedAt { get; set; }
        public Guid? LastModifiedBy { get; set; }
    }
}
