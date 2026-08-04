using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AuxiliumSoftware.AuxiliumServices.API.Models.DataEnumeratorValue
{
    public class ReorderValuesRequestModel
    {
        [Required]
        [JsonPropertyName("orderedValueIds")]
        public required List<Guid> OrderedValueIds { get; set; }
    }
}
