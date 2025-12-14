using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AuxiliumAPI.Models.Case
{
    public class PaginatedCasesResponseModel
    {
        [Required]
        [JsonPropertyName("cases")]
        public required List<CaseResponseModel> Cases { get; init; }

        [Required]
        [JsonPropertyName("page")]
        public required int Page { get; init; }

        [Required]
        [JsonPropertyName("perPage")]
        public required int PerPage { get; init; }

        [Required]
        [JsonPropertyName("total")]
        public required int Total { get; init; }

        [Required]
        [JsonPropertyName("totalPages")]
        public required int TotalPages { get; init; }

        [Required]
        [JsonPropertyName("hasMore")]
        public required bool HasMore { get; init; }
    }
}
