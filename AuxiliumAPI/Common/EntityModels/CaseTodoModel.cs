using AuxiliumAPI.Models.Case;

namespace AuxiliumAPI.Common.EntityModels
{
    public class CaseTodoModel
    {
        /**
         * The unique identifier for the additional property.
         */
        public Guid Id { get; set; }
        /**
         * The timestamp when the additional property was created.
         */
        public DateTime CreatedAt { get; set; }
        /**
         * The unique identifier of the user who created the additional property.
         */
        public Guid CreatedBy { get; set; }
        /**
         * The timestamp when the additional property was last updated.
         */
        public DateTime LastUpdatedAt { get; set; }
        /**
         * The unique identifier of the user who last updated the additional property.
         */
        public Guid LastUpdatedBy { get; set; }



        /**
         * The unique identifier for the case this todo is for.
         */
        public Guid CaseId { get; set; }



        /**
         * The summary/title of the todo item.
         */
        public string Summary { get; set; }
        /**
         * The detailed description of the todo item.
         */
        public string Description { get; set; }
        /**
         * The current status of the todo item.
         */
        public TodoStatusEnum Status{ get; set; }
        /**
         * The priority level of the todo item.
         */
        public TodoPriorityEnum Priority { get; set; }
        /**
         * An optional due date for the todo item.
         */
        public DateTime? DueDate { get; set; }
        /**
         * An optional unique identifier of the user this todo item is assigned to.
         */
        public Guid? AssignedTo { get; set; }
        /**
         * An optional reminder date for the todo item.
         */
        public DateTime? Reminder { get; set; }
        /**
         * The timestamp when the todo item was completed.
         */
        public DateTime? CompletedAt { get; set; }
        /**
         * The unique identifier of the user who completed the todo item.
         */
        public Guid? CompletedBy { get; set; }
        /**
         * An optional note added upon completion of the todo item.
         */
        public string? CompletionNote { get; set; }



        public UserModel? CreatedByUser { get; set; }
        public UserModel? LastUpdatedByUser { get; set; }
        public CaseModel? Case { get; set; }
        public UserModel? AssignedToUser { get; set; }
        public UserModel? CompletedByUser { get; set; }
    }
}
