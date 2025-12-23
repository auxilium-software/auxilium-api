namespace AuxiliumAPI.Common.EntityModels
{
    public class UserModel
    {
        public string Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; }
        public DateTime LastUpdatedAt { get; set; }
        public string LastUpdatedBy { get; set; }



        public string EmailAddress { get; set; }
        public string PasswordHash { get; set; }
        public bool IsAdmin { get; set; } = false;
        public bool IsCaseWorker { get; set; } = false;



        public UserModel CreatedByUser { get; set; }
        public UserModel LastUpdatedByUser { get; set; }
        public UserModel User { get; set; }
        public ICollection<CaseWorkerModel> WorkerOnCases { get; set; }
        public ICollection<CaseClientModel> ClientOnCases { get; set; }
        public ICollection<UserAdditionalProperty> AdditionalProperties { get; set; }
    }
}
