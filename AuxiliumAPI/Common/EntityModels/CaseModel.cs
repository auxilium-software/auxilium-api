using AuxiliumAPI.Models.Case;

namespace AuxiliumAPI.Common.EntityModels
{
    public class CaseModel
    {
        /// <summary>
        /// The unique identifier for the additional property.
        /// </summary>
        public required Guid Id { get; set; }
        /// <summary>
        /// The timestamp when the additional property was created.
        /// </summary>
        public required DateTime CreatedAt { get; set; }
        /// <summary>
        /// The unique identifier of the user who created the additional property.
        /// </summary>
        public required Guid CreatedBy { get; set; }
        /// <summary>
        /// The timestamp when the additional property was last updated.
        /// </summary>
        public DateTime? LastUpdatedAt { get; set; }
        /// <summary>
        /// The unique identifier of the user who last updated the additional property.
        /// </summary>
        public Guid? LastUpdatedBy { get; set; }



        /// <summary>
        /// The title of the case.
        /// </summary>
        public required string Title { get; set; }
        /// <summary>
        /// An optional description of the case.
        /// </summary>
        public required string? Description { get; set; }
        /// <summary>
        /// The sensitivity level of the case.
        /// </summary>
        public required CaseSensitivityEnum Sensitivity { get; set; } = CaseSensitivityEnum.Confidential;
        /// <summary>
        /// The current status of the case.
        /// </summary>
        public required CaseStatusEnum Status { get; set; } = CaseStatusEnum.Open;



        public UserModel? CreatedByUser { get; set; }
        public UserModel? LastUpdatedByUser { get; set; }
        public ICollection<CaseWorkerModel>? Workers { get; set; }
        public ICollection<CaseClientModel>? Clients { get; set; }
        public ICollection<CaseAdditionalPropertyModel>? AdditionalProperties { get; set; }
        public ICollection<CaseMessageModel>? Messages { get; set; }
        public ICollection<CaseFileModel>? Files { get; set; }
        public ICollection<CaseTodoModel>? Todos { get; set; }
        public ICollection<CaseTimelineItemModel>? Timeline { get; set; }
    }
}
