using AuxiliumAPI.Common.DataStructures;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AuxiliumAPI.Models.User
{
    public class UserResponseModel
    {
        [Required]
        [JsonPropertyName("id")]
        public required Guid ID { get; set; }



        [Required]
        [JsonPropertyName("createdAt")]
        public required DateTime CreatedAt { get; set; }

        [Required]
        [JsonPropertyName("createdBy")]
        public required Guid CreatedBy { get; set; }

        [Required]
        [JsonPropertyName("lastUpdatedAt")]
        public required DateTime? LastUpdatedAt { get; set; }

        [Required]
        [JsonPropertyName("lastUpdatedBy")]
        public required Guid? LastUpdatedBy { get; set; }



        [Required]
        [JsonPropertyName("fullName")]
        public required string FullName { get; set; }

        [Required]
        [JsonPropertyName("fullAddress")]
        public required string FullAddress { get; set; }

        [Required]
        [JsonPropertyName("telephoneNumber")]
        public required string TelephoneNumber { get; set; }

        [Required]
        [JsonPropertyName("gender")]
        public required string Gender { get; set; }

        [Required]
        [JsonPropertyName("dateOfBirth")]
        public required DateOnly? DateOfBirth { get; set; }



        [Required]
        [JsonPropertyName("additionalProperties")]
        public required Dictionary<string, AdditionalPropertySubStructure> AdditionalProperties { get; set; }

        [Required]
        [JsonPropertyName("files")]
        public required List<string> Files { get; set; }



        [Required]
        [JsonPropertyName("howDidYouFindOutAboutOurService")]
        public required string? HowDidYouFindOutAboutOurService { get; set; }



        [Required]
        [JsonPropertyName("emailAddress")]
        public required string EmailAddress { get; set; }

        [Required]
        [JsonPropertyName("isAdmin")]
        public required bool IsAdmin { get; set; } = false;

        [Required]
        [JsonPropertyName("isCaseWorker")]
        public required bool IsCaseWorker { get; set; } = false;
    }
}
