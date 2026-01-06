namespace AuxiliumAPI.Common.EntityModels
{
    public class CaseTimelineItemModel
    {
        /// <summary>
        /// The unique identifier for the timeline item.
        /// </summary>
        public required Guid Id { get; set; }
        /// <summary>
        /// The unique identifier for the case associated with this timeline item.
        /// </summary>
        public required Guid CaseId { get; set; }
        /// <summary>
        /// The timestamp when the timeline item was created.
        /// </summary>
        public required DateTime CreatedAt { get; set; }
        /// <summary>
        /// The unique identifier of the user who created the timeline item.
        /// </summary>
        public required Guid CreatedBy { get; set; }
        /// <summary>
        /// The timestamp when the timeline item was last updated.
        /// </summary>
        public DateTime? LastUpdatedAt { get; set; }
        /// <summary>
        /// The unique identifier of the user who last updated the timeline item.
        /// </summary>
        public Guid? LastUpdatedBy { get; set; }



        public CaseModel? Case { get; set; }
        public UserModel? CreatedByUser { get; set; }
        public UserModel? LastUpdatedByUser { get; set; }
    }
}
