namespace AuxiliumAPI.Common.EntityModels
{
    public class CaseClientModel
    {
        public Guid Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public Guid CreatedBy { get; set; }



        public Guid CaseId { get; set; }
        public Guid UserId { get; set; }



        public UserModel CreatedByUser { get; set; }
        public CaseModel Case { get; set; }
        public UserModel User { get; set; }
    }
}
