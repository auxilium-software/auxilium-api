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



        /**
         * The email address of the user.
         */
        public string EmailAddress { get; set; }
        /**
         * The hashed password of the user.
         */
        public string PasswordHash { get; set; }
        /**
         * The full name of the user.
         */
        public string FullName { get; set; }
        /**
         * The full address of the user.
         */
        public string FullAddress { get; set; }
        /**
         * The telephone number of the user.
         */
        public string TelephoneNumber { get; set; }
        /**
         * The gender of the user.
         */
        public string Gender { get; set; }
        /**
         * The date of birth of the user.
         */
        public DateOnly DateOfBirth { get; set; }
        /**
         * How the user found out about the service.
         */
        public string HowDidYouFindOutAboutOurService { get; set; }


        /**
         * Whether the user is allowed to log in.
         */
        public bool AllowLogin { get; set; }
        /**
         * Whether the user is an Administrator.
         */
        public bool IsAdmin { get; set; } = false;
        /**
         * Whether the user is a Case Worker.
         */
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
