using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AuxiliumSoftware.AuxiliumServices.API.Models.DataEnumeratorValue
{
    public class DataEnumeratorValueCreateRequestModel
    {
        [Required]
        [JsonPropertyName("displayName")]
        public required string DisplayName { get; set; }



        [JsonPropertyName("sortOrder")]
        public int? SortOrder { get; set; }
    }
}
