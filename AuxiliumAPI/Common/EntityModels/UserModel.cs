namespace AuxiliumAPI.Common.EntityModels
{
    public class UserModel
    {
        /// <summary>
        /// The unique identifier for the additional property.
        /// </summary>
        public required Guid Id { get; set; }
        /// <summary>
        /// The timestamp when the additional property was created.
        /// </summary>
        public required DateTime CreatedAt { get; set; }
        /// <summary>
        /// The unique identifier of the user who created the additional property.
        /// </summary>
        public Guid? CreatedBy { get; set; }
        /// <summary>
        /// The timestamp when the additional property was last updated.
        /// </summary>
        public DateTime? LastUpdatedAt { get; set; }
        /// <summary>
        /// The unique identifier of the user who last updated the additional property.
        /// </summary>
        public Guid? LastUpdatedBy { get; set; }



        /// <summary>
        /// The email address of the user.
        /// </summary>
        public required string? EmailAddress { get; set; }
        /// <summary>
        /// The hashed password of the user.
        /// </summary>
        public required string? PasswordHash { get; set; }
        /// <summary>
        /// The full name of the user.
        /// </summary>
        public required string FullName { get; set; }
        /// <summary>
        /// The full address of the user.
        /// </summary>
        public required string? FullAddress { get; set; }
        /// <summary>
        /// The telephone number of the user.
        /// </summary>
        public required string? TelephoneNumber { get; set; }
        /// <summary>
        /// The gender of the user.
        /// </summary>
        public required string? Gender { get; set; }
        /// <summary>
        /// The date of birth of the user.
        /// </summary>
        public required DateOnly? DateOfBirth { get; set; }
        /// <summary>
        /// How the user found out about the service.
        /// </summary>
        public required string? HowDidYouFindOutAboutOurService { get; set; }


        /// <summary>
        /// Whether the user is allowed to log in.
        /// </summary>
        public required bool AllowLogin { get; set; }
        /// <summary>
        /// Whether the user is an Administrator.
        /// </summary>
        public required bool IsAdmin { get; set; } = false;
        /// <summary>
        /// Whether the user is a Case Worker.
        /// </summary>
        public required bool IsCaseWorker { get; set; } = false;



        public UserModel? CreatedByUser { get; set; }
        public UserModel? LastUpdatedByUser { get; set; }
        public ICollection<CaseWorkerModel>? WorkerOnCases { get; set; }
        public ICollection<CaseClientModel>? ClientOnCases { get; set; }
        public ICollection<UserFileModel>? Files { get; set; }
        public ICollection<UserAdditionalPropertyModel>? AdditionalProperties { get; set; }
        public ICollection<RefreshTokenModel>? RefreshTokens { get; set; }
    }
}
