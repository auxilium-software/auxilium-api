using AuxiliumAPI.Common.DataStructures.CouchDB.SubStructures;
using CouchDB.Driver.Types;
using System.Text.Json.Serialization;

namespace AuxiliumAPI.Common.CouchDbDocumentConstruction.Structures
{
    public class UserDocumentStructure : CouchDocument
    {
        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("createdBy")]
        public required string CreatedBy { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTime UpdatedAt { get; set; }

        [JsonPropertyName("lastUpdatedBy")]
        public string? LastUpdatedBy { get; set; }



        [JsonPropertyName("fullName")]
        public required string FullName { get; set; }

        [JsonPropertyName("fullAddress")]
        public required string FullAddress { get; set; }

        [JsonPropertyName("telephoneNumber")]
        public required string TelephoneNumber { get; set; }

        [JsonPropertyName("gender")]
        public required string Gender { get; set; }

        [JsonPropertyName("dateOfBirth")]
        public DateOnly DateOfBirth { get; set; }

        [JsonPropertyName("howDidYouFindOutAboutOurService")]
        public string? HowDidYouFindOutAboutOurService { get; set; }

        [JsonPropertyName("preferences")]
        public Dictionary<string, object> Preferences { get; set; } = new();

        [JsonPropertyName("metadata")]
        public Dictionary<string, object> Metadata { get; set; } = new();


        [JsonPropertyName("additionalProperties")]
        public Dictionary<string, AdditionalPropertyStructure> AdditionalProperties { get; set; } = new();

        [JsonPropertyName("files")]
        public List<string> Files { get; set; } = new();


        public static UserDocumentStructure Create(
            string id,
            string createdBy,
            string fullName,
            string fullAddress,
            string telephoneNumber,
            string gender,
            DateTime dateOfBirth
            )
        {
            var now = DateTime.UtcNow;

            return new UserDocumentStructure
            {
                Id = id,
                CreatedBy = createdBy,
                CreatedAt = now,
                UpdatedAt = now,
                FullName = fullName,
                FullAddress = fullAddress,
                TelephoneNumber = telephoneNumber,
                Gender = gender,
                DateOfBirth = dateOfBirth
            };
        }
    }
}
