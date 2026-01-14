using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AuxiliumSoftware.AuxiliumServices.API.Models.User
{
    public class PaginatedUsersResponseModel
    {
        [Required]
        [JsonPropertyName("users")]
        public required List<UserResponseModel> Users { get; init; }

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
