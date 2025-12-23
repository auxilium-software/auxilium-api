namespace AuxiliumAPI.Common.EntityModels
{
    public class CaseMessageReadByModel
    {
        public Guid Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public Guid CreatedBy { get; set; }



        public Guid MessageId { get; set; }



        public UserModel CreatedByUser { get; set; }
        public CaseMessageModel Message { get; set; }
    }
}
