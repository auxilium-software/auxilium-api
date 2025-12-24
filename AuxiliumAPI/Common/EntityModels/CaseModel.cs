using AuxiliumAPI.Models.Case;

namespace AuxiliumAPI.Common.EntityModels
{
    public class CaseModel
    {
        /**
         * The unique identifier for the additional property.
         */
        public Guid Id { get; set; }
        /**
         * The timestamp when the additional property was created.
         */
        public DateTime CreatedAt { get; set; }
        /**
         * The unique identifier of the user who created the additional property.
         */
        public Guid CreatedBy { get; set; }
        /**
         * The timestamp when the additional property was last updated.
         */
        public DateTime LastUpdatedAt { get; set; }
        /**
         * The unique identifier of the user who last updated the additional property.
         */
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
