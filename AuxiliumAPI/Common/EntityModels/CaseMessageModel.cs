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



        public Guid CaseId { get; set; }
        public Guid SenderId { get; set; }
        public string Subject { get; set; }
        public string Content { get; set; }
        public bool IsUrgent { get; set; }



        public UserModel CreatedByUser { get; set; }
        public UserModel LastUpdatedByUser { get; set; }
        public CaseModel Case { get; set; }
        public UserModel Sender { get; set; }
        public ICollection<CaseMessageReadByModel> ReadBy { get; set; }
    }
}
