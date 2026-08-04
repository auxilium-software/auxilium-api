namespace AuxiliumSoftware.AuxiliumServices.API.Models.WEMWBS
{
    public class WEMWBSStatisticsResponseModel
    {
        public int TotalAssessments { get; set; }
        public double AverageScore { get; set; }
        public int LowestScore { get; set; }
        public int HighestScore { get; set; }
        public int LatestScore { get; set; }
        public string Trend { get; set; } = "N/A";
    }
}
