using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework.Enumerators;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AuxiliumSoftware.AuxiliumServices.API.Models.DataEnumerator
{
    public class DataEnumeratorCreationRequestModel
    {
        [JsonPropertyName("scope")]
        public DataEnumeratorScopeEnum Scope { get; set; }



        [Required]
        [JsonPropertyName("name")]
        public required string Name { get; set; }



        [JsonPropertyName("description")]
        public string? Description { get; set; }
    }
}
