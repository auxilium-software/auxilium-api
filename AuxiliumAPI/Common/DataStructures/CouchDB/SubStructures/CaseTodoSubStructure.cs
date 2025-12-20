using AuxiliumAPI.Models.Case;
using System.Text.Json.Serialization;

namespace AuxiliumAPI.Common.DataStructures.CouchDB.SubStructures
{
    public class CaseTodoSubStructure
    {
        [JsonPropertyName("id")]
        public required Guid Id { get; set; }

        [JsonPropertyName("createdAt")]
        public required DateTime CreatedAt { get; set; }

        [JsonPropertyName("createdBy")]
        public required Guid CreatedBy { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTime? UpdatedAt { get; set; }

        [JsonPropertyName("lastUpdatedBy")]
        public Guid? LastUpdatedBy { get; set; }




        [JsonPropertyName("summary")]
        public required string Summary { get; set; }

        [JsonPropertyName("description")]
        public required string Description { get; set; }

        [JsonPropertyName("status")]
        public required TodoStatusEnum Status { get; set; }

        [JsonPropertyName("priority")]
        public required TodoPriorityEnum Priority { get; set; }

        [JsonPropertyName("dueDate")]
        public required DateTime? DueDate { get; set; }

        [JsonPropertyName("assignedTo")]
        public required Guid? AssignedTo { get; set; }

        [JsonPropertyName("reminder")]
        public required DateTime? Reminder { get; set; }

        [JsonPropertyName("completedAt")]
        public required DateTime? CompletedAt { get; set; } = null;

        [JsonPropertyName("completedBy")]
        public required Guid? CompletedBy { get; set; } = null;

        [JsonPropertyName("completionNote")]
        public required string? CompletionNote { get; set; } = null;
    }
}
