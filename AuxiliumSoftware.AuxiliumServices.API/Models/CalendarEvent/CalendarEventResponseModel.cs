using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework.EntityModels;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AuxiliumSoftware.AuxiliumServices.API.Models.CalendarEvent
{
    public class CalendarEventResponseModel
    {
        [Required]
        [JsonPropertyName("id")]
        public required string Id { get; init; }

        [Required]
        [JsonPropertyName("source")]
        public required string Source { get; init; }

        [JsonPropertyName("sourceType")]
        public string? SourceType { get; init; }

        [JsonPropertyName("createdAt")]
        public DateTime? CreatedAt { get; init; }

        [JsonPropertyName("createdBy")]
        public Guid? CreatedBy { get; init; }

        [JsonPropertyName("lastUpdatedAt")]
        public DateTime? LastUpdatedAt { get; init; }

        [JsonPropertyName("lastUpdatedBy")]
        public Guid? LastUpdatedBy { get; init; }



        [Required]
        [JsonPropertyName("title")]
        public required string Title { get; init; }

        [Required]
        [JsonPropertyName("start")]
        public required DateTime Start { get; init; }

        [Required]
        [JsonPropertyName("end")]
        public required DateTime End { get; init; }

        [Required]
        [JsonPropertyName("allDay")]
        public required bool AllDay { get; init; }

        [JsonPropertyName("categoryId")]
        public Guid? CategoryId { get; init; }



        [JsonPropertyName("caseId")]
        public Guid? CaseId { get; init; }

        [JsonPropertyName("location")]
        public string? Location { get; init; }

        [JsonPropertyName("description")]
        public string? Description { get; init; }



        [Required]
        [JsonPropertyName("isOwner")]
        public required bool IsOwner { get; init; }

        [Required]
        [JsonPropertyName("isShareable")]
        public required bool IsShareable { get; init; }

        [JsonPropertyName("myInviteStatus")]
        public string? MyInviteStatus { get; init; }

        [JsonPropertyName("invites")]
        public List<CalendarEventInviteResponseModel>? Invites { get; init; }

        public static CalendarEventResponseModel FromEntity(
            CalendarEventEntityModel entity,
            Guid requestingUserId,
            IReadOnlyDictionary<Guid, string>? categoryNamesById,
            IReadOnlyDictionary<Guid, string>? userNamesById) => new()
            {
                Id = entity.Id.ToString(),
                Source = "manual",
                SourceType = null,
                CreatedAt = entity.CreatedAtUtc,
                CreatedBy = entity.CreatedByUserId,
                LastUpdatedAt = entity.LastUpdatedAtUtc,
                LastUpdatedBy = entity.LastUpdatedByUserId,
                Title = entity.Title,
                Start = entity.StartUtc,
                End = entity.EndUtc,
                AllDay = entity.IsAllDay,
                CategoryId = entity.CategoryValueId,
                CaseId = entity.CaseId,
                Location = entity.Location,
                Description = entity.Description,
                IsOwner = entity.CreatedByUserId == requestingUserId,
                IsShareable = true,
                MyInviteStatus = ResolveMyInviteStatus(entity, requestingUserId),
                Invites = entity.Invites?
                .Select(i => CalendarEventInviteResponseModel.FromEntity(i, userNamesById))
                .ToList() ?? new List<CalendarEventInviteResponseModel>(),
            };

        private static string? ResolveMyInviteStatus(CalendarEventEntityModel entity, Guid requestingUserId)
        {
            if (entity.CreatedByUserId == requestingUserId) return "owner";

            var myInvite = entity.Invites?.FirstOrDefault(i => i.InvitedUserId == requestingUserId);
            return myInvite?.Status.ToString().ToLowerInvariant();
        }
    }
}
