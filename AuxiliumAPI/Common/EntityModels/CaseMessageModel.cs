namespace AuxiliumAPI.Common.EntityModels
{
    public class CaseMessageModel
    {
        public Guid Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime LastUpdatedAt { get; set; }
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
