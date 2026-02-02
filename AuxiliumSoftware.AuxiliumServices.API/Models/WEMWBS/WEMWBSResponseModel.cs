namespace AuxiliumSoftware.AuxiliumServices.API.Models.WEMWBS
{
    public class WEMWBSResponseModel
    {
        public Guid Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public Guid? CreatedBy { get; set; }
        public int TotalScore { get; set; }
        public WEMWBSScoresModel Scores { get; set; } = new();
    }
}
