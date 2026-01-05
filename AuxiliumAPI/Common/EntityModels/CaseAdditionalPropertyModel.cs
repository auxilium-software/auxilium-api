namespace AuxiliumAPI.Common.EntityModels
{
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
        public required Guid CaseId { get; set; }
        /**
         * The name of the additional property.
         */
        public required string Name { get; set; }
        /**
         * The MIME type of the additional property (e.g., "text/plain", "application/json").
         */
        public required string ContentType { get; set; }
        /**
         * The actual content of the additional property.
         */
        public required string Content { get; set; }



        public UserModel CreatedByUser { get; set; }
        public UserModel LastUpdatedByUser { get; set; }
        public CaseModel Case { get; set; }
    }
}
