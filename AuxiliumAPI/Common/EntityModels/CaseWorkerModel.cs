namespace AuxiliumAPI.Common.EntityModels
{
    public class CaseWorkerModel
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
        /// The unique identifier of the case this assignment is for.
        /// </summary>
        public required Guid CaseId { get; set; }
        /// <summary>
        /// The unique identifier of the user assigned to the case.
        /// </summary>
        public required Guid UserId { get; set; }



        public UserModel? CreatedByUser { get; set; }
        public CaseModel? Case { get; set; }
        public UserModel? User { get; set; }
    }
}
