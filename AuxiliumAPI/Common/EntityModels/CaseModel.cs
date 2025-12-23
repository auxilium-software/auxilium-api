using AuxiliumAPI.Models.Case;

namespace AuxiliumAPI.Common.EntityModels
{
    public class CaseModel
    {
        public Guid Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime LastUpdatedAt { get; set; }
        public Guid LastUpdatedBy { get; set; }



        public string Title { get; set; }
        public string? Description { get; set; }
        public CaseSensitivityEnum Sensitivity { get; set; } = CaseSensitivityEnum.Confidential;
        public CaseStatusEnum Status { get; set; } = CaseStatusEnum.Open;



        public UserModel? CreatedByUser { get; set; }
        public UserModel? LastUpdatedByUser { get; set; }
        public ICollection<CaseWorkerModel> Workers { get; set; }
        public ICollection<CaseClientModel> Clients { get; set; }
        public ICollection<CaseAdditionalPropertyModel> AdditionalProperties { get; set; }
        public ICollection<CaseMessageModel> Messages { get; set; }
        public ICollection<CaseFileModel> Files { get; set; }
        public ICollection<CaseTodoModel> Todos { get; set; }
        public ICollection<CaseTimelineItemModel> Timeline { get; set; }
    }
}
