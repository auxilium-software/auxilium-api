namespace AuxiliumAPI.Common.EntityModels
{
    public class UserModel
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



        public string EmailAddress { get; set; }
        public string PasswordHash { get; set; }
        public string FullName { get; set; }
        public string FullAddress { get; set; }
        public string TelephoneNumber { get; set; }
        public string Gender { get; set; }
        public DateOnly DateOfBirth { get; set; }
        public string HowDidYouFindOutAboutOurService { get; set; }


        public bool AllowLogin { get; set; }
        public bool IsAdmin { get; set; } = false;
        public bool IsCaseWorker { get; set; } = false;



        public UserModel CreatedByUser { get; set; }
        public UserModel LastUpdatedByUser { get; set; }
        public UserModel User { get; set; }
        public ICollection<CaseWorkerModel> WorkerOnCases { get; set; }
        public ICollection<CaseClientModel> ClientOnCases { get; set; }
        public ICollection<UserFileModel> Files { get; set; }
        public ICollection<UserAdditionalPropertyModel> AdditionalProperties { get; set; }
        public ICollection<RefreshTokenModel> RefreshTokens { get; set; }
    }
}
