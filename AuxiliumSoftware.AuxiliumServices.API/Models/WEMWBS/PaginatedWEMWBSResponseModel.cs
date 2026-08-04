namespace AuxiliumSoftware.AuxiliumServices.API.Models.WEMWBS
{
    public class PaginatedWEMWBSResponseModel
    {
        public List<WEMWBSResponseModel> Assessments { get; set; } = new();
        public int Total { get; set; }
        public int Page { get; set; }
        public int PerPage { get; set; }
        public int TotalPages { get; set; }
        public bool HasMore { get; set; }
    }
}
