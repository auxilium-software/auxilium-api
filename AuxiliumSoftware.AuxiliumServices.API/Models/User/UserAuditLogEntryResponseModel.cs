using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework.Enumerators;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AuxiliumSoftware.AuxiliumServices.API.Models.User
{
    public class UserAuditLogEntryResponseModel
    {
        [Key]
        [Required]
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [Required]
        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        [Required]
        [JsonPropertyName("createdBy")]
        public Guid? CreatedBy { get; set; }

        [Required]
        [JsonPropertyName("createdByName")]
        public string? CreatedByName { get; set; }

        [Required]
        [JsonPropertyName("targetUserId")]
        public Guid? TargetUserId { get; set; }

        [Required]
        [JsonPropertyName("targetUserIdName")]
        public string? TargetUserIdName { get; set; }

        [Required]
        [JsonPropertyName("entityType")]
        public UserEntityTypeEnum EntityType { get; set; }

        [Required]
        [JsonPropertyName("entityId")]
        public Guid? EntityId { get; set; }

        [Required]
        [JsonPropertyName("action")]
        public AuditLogActionTypeEnum Action { get; set; }




        [Required]
        [JsonPropertyName("propertyName")]
        public string? PropertyName { get; set; }

        [Required]
        [JsonPropertyName("previousValue")]
        public string? PreviousValue { get; set; }

        [Required]
        [JsonPropertyName("newValue")]
        public string? NewValue { get; set; }








        [Required]
        [JsonPropertyName("description")]
        public string? Description { get; set; }
    }
}
