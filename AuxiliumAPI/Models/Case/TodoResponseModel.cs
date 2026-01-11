using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AuxiliumAPI.Models.Case
{
    public class TodoResponseModel
    {
        [Required]
        [JsonPropertyName("id")]
        public required Guid Id { get; init; }

        [Required]
        [JsonPropertyName("caseId")]
        public required Guid CaseId { get; init; }

        [Required]
        [JsonPropertyName("createdAt")]
        public required DateTime CreatedAt { get; init; }

        [Required]
        [JsonPropertyName("createdBy")]
        public required Guid? CreatedBy { get; init; }



        [Required]
        [JsonPropertyName("summary")]
        public required string Summary { get; init; }

        [JsonPropertyName("description")]
        public string? Description { get; init; }

        [Required]
        [JsonPropertyName("status")]
        public required TodoStatusEnum Status { get; init; }

        [Required]
        [JsonPropertyName("priority")]
        public required TodoPriorityEnum Priority { get; init; }



        [JsonPropertyName("dueDate")]
        public DateTime? DueDate { get; init; }

        [JsonPropertyName("completedAt")]
        public DateTime? CompletedAt { get; init; }

        [JsonPropertyName("completedBy")]
        public Guid? CompletedBy { get; init; }

        [JsonPropertyName("completionNote")]
        public string? CompletionNote { get; init; }

        [JsonPropertyName("assignedTo")]
        public Guid? AssignedTo { get; init; }
    }
}
