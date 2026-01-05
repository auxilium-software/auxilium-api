namespace AuxiliumAPI.Common.EntityModels
{
    public class CaseMessageReadByModel
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
        /// The unique identifier for the message that has been read.
        /// </summary>
        public Guid MessageId { get; set; }



        public UserModel CreatedByUser { get; set; }
        public CaseMessageModel Message { get; set; }
    }
}
