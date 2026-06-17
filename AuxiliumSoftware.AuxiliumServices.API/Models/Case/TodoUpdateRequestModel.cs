using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework;
using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework.EntityModels;
using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework.Enumerators;
using System.Text.Json.Serialization;

namespace AuxiliumSoftware.AuxiliumServices.API.Models.Case
{
    public class TodoUpdateRequestModel
    {
        [JsonPropertyName("summary")]
        public string? Summary { get; set; }



        [JsonPropertyName("description")]
        public string? Description { get; set; }



        [JsonPropertyName("priority")]
        public TodoPriorityEnum? Priority { get; set; }



        [JsonPropertyName("dueDate")]
        public DateTime? DueDate { get; set; }



        [JsonPropertyName("assignedTo")]
        public Guid? AssignedTo { get; set; }



        [JsonPropertyName("reminder")]
        public DateTime? Reminder { get; set; }



        [JsonPropertyName("status")]
        public TodoStatusEnum? Status { get; set; }



        [JsonPropertyName("completionNote")]
        public string? CompletionNote { get; set; }
    }
}
