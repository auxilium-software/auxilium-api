using AuxiliumSoftware.AuxiliumServices.Common.Enumerators;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AuxiliumAPI.Models.Case
{
    public class CaseUpdateRequestModel
    {
        [JsonPropertyName("title")]
        public string? Title { get; init; }

        [JsonPropertyName("description")]
        public string? Description { get; init; }

        [JsonPropertyName("status")]
        public CaseStatusEnum? Status { get; init; }

        [JsonPropertyName("sensitivity")]
        public CaseSensitivityEnum? Sensitivity { get; init; }
    }
}
