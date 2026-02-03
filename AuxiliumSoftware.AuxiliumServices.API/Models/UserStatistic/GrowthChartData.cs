namespace AuxiliumSoftware.AuxiliumServices.API.Models.UserStatistic
{
    public class GrowthChartData
    {
        public List<string> Labels { get; set; } = new();
        public List<int> Values { get; set; } = new();
        public List<int> CumulativeValues { get; set; } = new();
    }
}
