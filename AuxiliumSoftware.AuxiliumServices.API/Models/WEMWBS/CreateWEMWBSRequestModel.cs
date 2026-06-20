using System.ComponentModel.DataAnnotations;

namespace AuxiliumSoftware.AuxiliumServices.API.Models.WEMWBS
{
    public class CreateWEMWBSRequestModel
    {
        [Required]
        public required Guid SubjectUserId { get; set; }

        [Required]
        public required WemwbsAssessmentResponsesSubmodel Responses { get; set; }
    }
}
