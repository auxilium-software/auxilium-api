using AuxiliumAPI.Common.DataStructures.CouchDB.SubStructures;
using CouchDB.Driver.Types;
using System.Text.Json.Serialization;

namespace AuxiliumAPI.Common.DataStructures.CouchDB
{
    public class UserDocumentStructure : CouchDocument
    {
        [JsonPropertyName("_id")]
        public new string Id
        {
            get => base.Id;
            set => base.Id = value;
        }

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("createdBy")]
        public required Guid CreatedBy { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTime LastUpdatedAt { get; set; }

        [JsonPropertyName("lastUpdatedBy")]
        public Guid? LastUpdatedBy { get; set; }



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
        public Dictionary<string, AdditionalPropertySubStructure> AdditionalProperties { get; set; } = new();

        [JsonPropertyName("files")]
        public List<string> Files { get; set; } = [];
    }
}
