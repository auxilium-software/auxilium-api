namespace AuxiliumAPI.Common.EntityModels
{
    public class UserAdditionalPropertyModel
    {
        /// <summary>
        /// The unique identifier for the additional property.
        /// </summary>
        public Guid Id { get; set; }
        /// <summary>
        /// The timestamp when the additional property was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }
        /// <summary>
        /// The unique identifier of the user who created the additional property.
        /// </summary>
        public Guid CreatedBy { get; set; }
        /// <summary>
        /// The timestamp when the additional property was last updated.
        /// </summary>
        public DateTime LastUpdatedAt { get; set; }
        /// <summary>
        /// The unique identifier of the user who last updated the additional property.
        /// </summary>
        public Guid LastUpdatedBy { get; set; }



        /// <summary>
        /// The unique identifier of the user this additional property is for.
        /// </summary>
        public required  Guid UserId { get; set; }
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



        public UserModel CreatedByUser { get; set; }
        public UserModel LastUpdatedByUser { get; set; }
        public UserModel User { get; set; }
    }
}
