namespace AuxiliumSoftware.AuxiliumServices.API.Models.SystemSettings
{
    public class AllSystemSettingsResponseModel
    {
        public List<SystemSettingItemResponseModel> Settings { get; set; } = new();
        public int TotalCount { get; set; }
    }
}
