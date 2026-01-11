namespace AuxiliumAPI.Common.EntityModels
{
    public class WEMWBSModel
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



        public required int OptimismScore { get; set; }
        public required int UsefulnessScore { get; set; }
        public required int RelaxedScore { get; set; }
        public required int InterestedInPeopleScore { get; set; }
        public required int SpareEnergyScore { get; set; }
        public required int ProblemHandlingScore { get; set; }
        public required int ClearThoughtScore { get; set; }
        public required int FeelingGoodSelfScore { get; set; }
        public required int FeelingCloseToPeopleScore { get; set; }
        public required int ConfidenceScore { get; set; }
        public required int MakingUpOwnMindScore { get; set; }
        public required int FeelingLovedScore { get; set; }
        public required int InterestedInNewThingsScore { get; set; }
        public required int FeelingCheerfulScore { get; set; }



        public UserModel? CreatedByUser { get; set; }
    }
}
