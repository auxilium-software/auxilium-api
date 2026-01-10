namespace AuxiliumAPI.Common.EntityModels
{
    public class RefreshTokenModel
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
        /// A hash of the Refresh Token.
        /// </summary>
        public required string TokenHash { get; set; }
        /// <summary>
        /// The expiration datetime of the Refresh Token.
        /// </summary>
        public required DateTime ExpiresAt { get; set; }



        public UserModel? CreatedByUser { get; set; }
        public UserModel? User { get; set; }
    }
}
