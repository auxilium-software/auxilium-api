using AuxiliumAPI.Models.Case;

namespace AuxiliumAPI.Common.EntityModels
{
    public class CaseTodoModel
    {
        /// <summary>
        /// The unique identifier for the additional property.
        /// </summary>
        public Guid Id { get; set; }
        /// <summary>
        /// The timestamp when the additional property was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }
        /// <summary>
        /// The unique identifier of the user who created the additional property.
        /// </summary>
        public Guid CreatedBy { get; set; }
        /// <summary>
        /// The timestamp when the additional property was last updated.
        /// </summary>
        public DateTime LastUpdatedAt { get; set; }
        /// <summary>
        /// The unique identifier of the user who last updated the additional property.
        /// </summary>
        public Guid LastUpdatedBy { get; set; }



        /// <summary>
        /// The unique identifier for the case this todo is for.
        /// </summary>
        public Guid CaseId { get; set; }



        /// <summary>
        /// The summary/title of the todo item.
        /// </summary>
        public string Summary { get; set; }
        /// <summary>
        /// The detailed description of the todo item.
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// The current status of the todo item.
        /// </summary>
        public TodoStatusEnum Status { get; set; }
        /// <summary>
        /// The priority level of the todo item.
        /// </summary>
        public TodoPriorityEnum Priority { get; set; }
        /// <summary>
        /// An optional due date for the todo item.
        /// </summary>
        public DateTime? DueDate { get; set; }
        /// <summary>
        /// An optional unique identifier of the user this todo item is assigned to.
        /// </summary>
        public Guid? AssignedTo { get; set; }
        /// <summary>
        /// An optional reminder date for the todo item.
        /// </summary>
        public DateTime? Reminder { get; set; }
        /// <summary>
        /// The timestamp when the todo item was completed.
        /// </summary>
        public DateTime? CompletedAt { get; set; }
        /// <summary>
        /// The unique identifier of the user who completed the todo item.
        /// </summary>
        public Guid? CompletedBy { get; set; }
        /// <summary>
        /// An optional note added upon completion of the todo item.
        /// </summary>
        public string? CompletionNote { get; set; }



        public UserModel? CreatedByUser { get; set; }
        public UserModel? LastUpdatedByUser { get; set; }
        public CaseModel? Case { get; set; }
        public UserModel? AssignedToUser { get; set; }
        public UserModel? CompletedByUser { get; set; }
    }
}
