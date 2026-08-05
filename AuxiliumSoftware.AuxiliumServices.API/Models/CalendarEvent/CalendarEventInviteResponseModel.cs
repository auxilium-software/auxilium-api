using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework.EntityModels;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AuxiliumSoftware.AuxiliumServices.API.Models.CalendarEvent
{
    public class CalendarEventInviteResponseModel
    {
        [Required]
        [JsonPropertyName("userId")]
        public required Guid? UserId { get; init; }

        [Required]
        [JsonPropertyName("status")]
        public required string Status { get; init; }

        [Required]
        [JsonPropertyName("invitedAt")]
        public required DateTime InvitedAt { get; init; }

        [JsonPropertyName("respondedAt")]
        public DateTime? RespondedAt { get; init; }

        public static CalendarEventInviteResponseModel FromEntity(
            CalendarEventInviteEntityModel entity,
            IReadOnlyDictionary<Guid, string>? userNamesById) => new()
            {
                UserId = entity.InvitedUserId,
                Status = entity.Status.ToString().ToLowerInvariant(),
                InvitedAt = entity.InvitedAtUtc,
                RespondedAt = entity.RespondedAtUtc,
            };
    }
}
