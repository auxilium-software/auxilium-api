namespace AuxiliumAPI.Common.EntityModels
{
    public class CaseModel
    {
        public string Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; }
        public DateTime LastUpdatedAt { get; set; }
        public string LastUpdatedBy { get; set; }



        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Sensitivity { get; set; } = "confidential";
        public string Status { get; set; } = "open";



        public UserModel? CreatedByUser { get; set; }
        public UserModel? LastUpdatedByUser { get; set; }
        public ICollection<CaseWorkerModel> Workers { get; set; }
        public ICollection<CaseClientModel> Clients { get; set; }
        public ICollection<CaseAdditionalProperty> AdditionalProperties { get; set; }
        public ICollection<CaseMessageModel> Messages { get; set; }
        public ICollection<CaseFileModel> Files { get; set; }
    }
}
