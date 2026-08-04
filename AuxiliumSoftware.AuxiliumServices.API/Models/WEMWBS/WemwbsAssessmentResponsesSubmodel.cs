using System.ComponentModel.DataAnnotations;

namespace AuxiliumSoftware.AuxiliumServices.API.Models.WEMWBS
{
    public class WemwbsAssessmentResponsesSubmodel
    {
        [Required]
        [Range(1, 5)]
        public required int OptimismScore { get; set; }

        [Required]
        [Range(1, 5)]
        public required int UsefulnessScore { get; set; }

        [Required]
        [Range(1, 5)]
        public required int RelaxedScore { get; set; }

        [Required]
        [Range(1, 5)]
        public required int InterestedInPeopleScore { get; set; }

        [Required]
        [Range(1, 5)]
        public required int SpareEnergyScore { get; set; }

        [Required]
        [Range(1, 5)]
        public required int ProblemHandlingScore { get; set; }

        [Required]
        [Range(1, 5)]
        public required int ClearThoughtScore { get; set; }

        [Required]
        [Range(1, 5)]
        public required int FeelingGoodSelfScore { get; set; }

        [Required]
        [Range(1, 5)]
        public required int FeelingCloseToPeopleScore { get; set; }

        [Required]
        [Range(1, 5)]
        public required int ConfidenceScore { get; set; }

        [Required]
        [Range(1, 5)]
        public required int MakingUpOwnMindScore { get; set; }

        [Required]
        [Range(1, 5)]
        public required int FeelingLovedScore { get; set; }

        [Required]
        [Range(1, 5)]
        public required int InterestedInNewThingsScore { get; set; }

        [Required]
        [Range(1, 5)]
        public required int FeelingCheerfulScore { get; set; }
    }
}
