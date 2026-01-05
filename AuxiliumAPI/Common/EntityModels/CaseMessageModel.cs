namespace AuxiliumAPI.Common.EntityModels
{
    public class CaseMessageModel
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
        /// The unique identifier of the case this message is attached to.
        /// </summary>
        public Guid CaseId { get; set; }
        /// <summary>
        /// The unique identifier of the user who sent the message.
        /// </summary>
        public Guid SenderId { get; set; }
        /// <summary>
        /// The subject of the message.
        /// </summary>
        public string Subject { get; set; }
        /// <summary>
        /// The content/body of the message.
        /// </summary>
        public string Content { get; set; }
        /// <summary>
        /// Indicates whether the message is marked as urgent.
        /// </summary>
        public bool IsUrgent { get; set; }



        public UserModel CreatedByUser { get; set; }
        public UserModel LastUpdatedByUser { get; set; }
        public CaseModel Case { get; set; }
        public UserModel Sender { get; set; }
        public ICollection<CaseMessageReadByModel> ReadBy { get; set; }
    }
}