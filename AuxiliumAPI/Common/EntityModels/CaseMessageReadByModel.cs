namespace AuxiliumAPI.Common.EntityModels
{
    public class CaseMessageReadByModel
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
         * The unique identifier for the message that has been read.
         */
        public Guid MessageId { get; set; }



        public UserModel CreatedByUser { get; set; }
        public CaseMessageModel Message { get; set; }
    }
}
