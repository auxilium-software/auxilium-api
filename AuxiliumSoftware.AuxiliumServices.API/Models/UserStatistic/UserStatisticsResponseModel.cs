namespace AuxiliumSoftware.AuxiliumServices.API.Models.UserStatistic
{
    public class UserStatisticsResponseModel
    {
        public int TotalUsers { get; set; }
        public double GrowthPercentage { get; set; }
        public int UsersCreatedThisMonth { get; set; }
        public int UsersCreatedLastMonth { get; set; }

        public GrowthChartData GrowthChart { get; set; } = new();
        public List<LanguageDistributionItem> LanguageDistribution { get; set; } = new();
        public UserTypeBreakdownModel UserTypeBreakdown { get; set; } = new();

        public DateTime GeneratedAt { get; set; }
    }
}
