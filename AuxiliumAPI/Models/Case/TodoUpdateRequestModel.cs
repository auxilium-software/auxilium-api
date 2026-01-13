using AuxiliumServices.Common.Enumerators;

namespace AuxiliumAPI.Models.Case
{
    public class TodoUpdateRequestModel
    {
        public string? Summary { get; set; }
        public string? Description { get; set; }
        public TodoPriorityEnum? Priority { get; set; }
        public DateTime? DueDate { get; set; }
        public Guid? AssignedTo { get; set; }
        public DateTime? Reminder { get; set; }
        public TodoStatusEnum? Status { get; set; }
        public string? CompletionNote { get; set; }
    }
}
