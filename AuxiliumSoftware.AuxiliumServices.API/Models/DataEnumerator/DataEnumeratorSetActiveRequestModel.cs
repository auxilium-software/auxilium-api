using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AuxiliumSoftware.AuxiliumServices.API.Models.DataEnumerator
{
    public class DataEnumeratorSetActiveRequestModel
    {
        [Required]
        [JsonPropertyName("isActive")]
        public required bool IsActive { get; set; }
    }
}
