using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework;
using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework.EntityModels;
using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework.Enumerators;

namespace AuxiliumSoftware.AuxiliumServices.API.Models.Case
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
