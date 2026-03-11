namespace AuxiliumSoftware.AuxiliumServices.API.Models.Waf.ResponseModels.SubResponseModels
{
    public class HourlyBreakdownItem
    {
        public required DateTime Hour { get; set; }
        public required int TotalAttempts { get; set; }
        public required int FailedAttempts { get; set; }
        public required int BlockedAttempts { get; set; }
    }
}
