using AuxiliumSoftware.AuxiliumServices.Common.Enumerators;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AuxiliumAPI.Models.Case
{
    public class TodoCreationRequestModel
    {
        [Required]
        [JsonPropertyName("summary")]
        public required string Summary { get; init; }



        [JsonPropertyName("description")]
        public string? Description { get; init; }

        [JsonPropertyName("priority")]
        public TodoPriorityEnum Priority { get; init; }

        [JsonPropertyName("dueDate")]
        public DateTime? DueDate { get; init; }

        [JsonPropertyName("assignedTo")]
        public Guid? AssignedTo { get; init; }

        [JsonPropertyName("reminder")]
        public DateTime? Reminder { get; init; }
    }
}
