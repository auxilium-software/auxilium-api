using System.ComponentModel.DataAnnotations;

namespace AuxiliumSoftware.AuxiliumServices.API.Models.WEMWBS
{
    public class CreateWEMWBSRequestModel
    {
        [Required]
        [Range(1, 5)]
        public int OptimismScore { get; set; }

        [Required]
        [Range(1, 5)]
        public int UsefulnessScore { get; set; }

        [Required]
        [Range(1, 5)]
        public int RelaxedScore { get; set; }

        [Required]
        [Range(1, 5)]
        public int InterestedInPeopleScore { get; set; }

        [Required]
        [Range(1, 5)]
        public int SpareEnergyScore { get; set; }

        [Required]
        [Range(1, 5)]
        public int ProblemHandlingScore { get; set; }

        [Required]
        [Range(1, 5)]
        public int ClearThoughtScore { get; set; }

        [Required]
        [Range(1, 5)]
        public int FeelingGoodSelfScore { get; set; }

        [Required]
        [Range(1, 5)]
        public int FeelingCloseToPeopleScore { get; set; }

        [Required]
        [Range(1, 5)]
        public int ConfidenceScore { get; set; }

        [Required]
        [Range(1, 5)]
        public int MakingUpOwnMindScore { get; set; }

        [Required]
        [Range(1, 5)]
        public int FeelingLovedScore { get; set; }

        [Required]
        [Range(1, 5)]
        public int InterestedInNewThingsScore { get; set; }

        [Required]
        [Range(1, 5)]
        public int FeelingCheerfulScore { get; set; }
    }
}
