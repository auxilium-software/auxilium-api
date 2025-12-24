namespace AuxiliumAPI.Common.EntityModels
{
    /// <summary>
    /// Represents an assignment of a client to a case.
    /// </summary>
    public class CaseClientModel
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
         * The unique identifier of the case this assignment is for.
         */
        public Guid CaseId { get; set; }
        /**
         * The unique identifier of the user assigned to the case.
         */
        public Guid UserId { get; set; }



        public UserModel CreatedByUser { get; set; }
        public CaseModel Case { get; set; }
        public UserModel User { get; set; }
    }
}
