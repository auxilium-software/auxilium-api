namespace AuxiliumAPI.Common.EntityModels
{
    /// <summary>
    /// Represents an additional property associated with a case, including metadata, content, and related user and case information.
    /// </summary>
    /// 
    /// <remarks>
    /// This model is used to store and manage custom properties for a case, such as metadata or
    /// additional content. It includes information about the property itself, its associated case, and the users who
    /// created or last updated it.
    /// </remarks>
    public class CaseAdditionalPropertyModel
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



        /**
         * The unique identifier of the case this additional property is for.
         */
        public Guid CaseId { get; set; }
        public string PropertyKey { get; set; }
        public string OriginalName { get; set; }
        public string PrettyName { get; set; }
        public string URLSlug { get; set; }
        /**
         * The MIME type of the additional property (e.g., "text/plain", "application/json").
         */
        public string ContentType { get; set; }
        /**
         * The actual content of the additional property.
         */
        public string Content { get; set; }



        public UserModel CreatedByUser { get; set; }
        public UserModel LastUpdatedByUser { get; set; }
        public CaseModel Case { get; set; }
    }
}
