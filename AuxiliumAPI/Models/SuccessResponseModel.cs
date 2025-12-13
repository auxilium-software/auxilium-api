using System.ComponentModel.DataAnnotations;

namespace AuxiliumAPI.Models
{
    public class SuccessResponseModel
    {
        [Required]
        public string Status { get; init; } = "success";
    }
}
