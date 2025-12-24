namespace AuxiliumAPI.Common.EntityModels
{
    public class RefreshTokenModel
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



        public string TokenHash { get; set; }
        public DateTime ExpiresAt { get; set; }



        public UserModel CreatedByUser { get; set; }
        public UserModel User { get; set; }
    }
}
