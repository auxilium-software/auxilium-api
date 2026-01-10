namespace AuxiliumAPI.Common.EntityModels
{
    public class CaseAdditionalPropertyModel
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
        /// The unique identifier of the case this additional property is for.
        /// </summary>
        public required Guid CaseId { get; set; }
        /// <summary>
        /// The name of the additional property.
        /// </summary>
        public required string Name { get; set; }
        /// <summary>
        /// The MIME type of the additional property (e.g., "text/plain", "application/json").
        /// </summary>
        public required string ContentType { get; set; }
        /// <summary>
        /// The actual content of the additional property.
        /// </summary>
        public required string Content { get; set; }



        public UserModel? CreatedByUser { get; set; }
        public UserModel? LastUpdatedByUser { get; set; }
        public CaseModel? Case { get; set; }
    }
}
