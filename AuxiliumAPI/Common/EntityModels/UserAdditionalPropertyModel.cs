namespace AuxiliumAPI.Common.EntityModels
{
    public class UserAdditionalPropertyModel
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



        public Guid UserId { get; set; }
        public string PropertyKey { get; set; }
        public string OriginalName { get; set; }
        public string PrettyName { get; set; }
        public string URLSlug { get; set; }
        public string ContentType { get; set; }
        public string Content { get; set; }



        public UserModel CreatedByUser { get; set; }
        public UserModel LastUpdatedByUser { get; set; }
        public UserModel User { get; set; }
    }
}
