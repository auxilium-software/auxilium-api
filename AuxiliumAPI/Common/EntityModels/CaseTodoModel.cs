using AuxiliumAPI.Models.Case;

namespace AuxiliumAPI.Common.EntityModels
{
    public class CaseTodoModel
    {
        public Guid Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime LastUpdatedAt { get; set; }
        public Guid LastUpdatedBy { get; set; }



        public Guid CaseId { get; set; }



        public string Summary { get; set; }
        public string Description { get; set; }
        public TodoStatusEnum Status{ get; set; }
        public TodoPriorityEnum Priority { get; set; }
        public DateTime? DueDate { get; set; }
        public Guid? AssignedTo { get; set; }
        public DateTime? Reminder { get; set; }
        public DateTime? CompletedAt { get; set; }
        public Guid? CompletedBy { get; set; }
        public string? CompletionNote { get; set; }



        public UserModel? CreatedByUser { get; set; }
        public UserModel? LastUpdatedByUser { get; set; }
        public CaseModel? Case { get; set; }
        public UserModel? AssignedToUser { get; set; }
        public UserModel? CompletedByUser { get; set; }
    }
}
