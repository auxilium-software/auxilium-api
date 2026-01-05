namespace AuxiliumAPI.Common.EntityModels
{
    public class CaseMessageModel
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
         * The unique identifier of the case this message is attached to.
         */
        public Guid CaseId { get; set; }
        /**
         * The unique identifier of the user who sent the message.
         */
        public Guid SenderId { get; set; }
        /**
         * The subject of the message.
         */
        public string Subject { get; set; }
        /**
         * The content/body of the message.
         */
        public string Content { get; set; }
        /**
         * Indicates whether the message is marked as urgent.
         */
        public bool IsUrgent { get; set; }



        public UserModel CreatedByUser { get; set; }
        public UserModel LastUpdatedByUser { get; set; }
        public CaseModel Case { get; set; }
        public UserModel Sender { get; set; }
        public ICollection<CaseMessageReadByModel> ReadBy { get; set; }
    }
}
