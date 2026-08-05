using AuxiliumSoftware.AuxiliumServices.API.Common;
using AuxiliumSoftware.AuxiliumServices.API.Common.ControllerBases;
using AuxiliumSoftware.AuxiliumServices.API.Models;
using AuxiliumSoftware.AuxiliumServices.API.Models.CalendarEvent;
using AuxiliumSoftware.AuxiliumServices.API.Models.CalendarEventCategory;
using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework;
using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework.EntityModels;
using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework.Enumerators;
using AuxiliumSoftware.AuxiliumServices.Common.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuxiliumSoftware.AuxiliumServices.API.Controllers
{

    [ApiController]
    [Route("/api/v3/calendar-events")]
    [Tags("Calendar")]
    [Authorize]
    public class CalendarController : LoggedInControllerBase
    {
        private readonly IDataEnumeratorService _dataEnumeratorService;

        public CalendarController(
            ISystemSettingsService systemSettingsService,
            IConfiguration configuration,
            AuxiliumDbContext db,
            IWebApplicationFirewallService waf,
            ILogger<CalendarController> logger,
            ITotpService totpService,

            IDataEnumeratorService dataEnumeratorService
            )
            : base(systemSettingsService, configuration, db, waf, logger, totpService)
        {
            _dataEnumeratorService = dataEnumeratorService;
        }

        #region Events

        [HttpGet("")]
        [ProducesResponseType(typeof(List<CalendarEventResponseModel>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<CalendarEventResponseModel>>> GetEvents(
            [FromQuery] DateTime start,
            [FromQuery] DateTime end,
            [FromQuery] Guid? categoryId = null,
            CancellationToken ct = default)
        {
            try
            {
                var (user, error) = await RequireCalendarAccessAsync();
                if (error != null) return error;

                if (end <= start)
                {
                    return StatusCode(StatusCodes.Status400BadRequest, new FailureResponseModel { Detail = "end must be after start" });
                }

                var maxRange = TimeSpan.FromDays(120);
                if (end - start > maxRange)
                {
                    return StatusCode(StatusCodes.Status400BadRequest, new FailureResponseModel
                    {
                        Detail = $"Requested range exceeds the maximum of {maxRange.TotalDays:0} days"
                    });
                }

                var startUtc = start.ToUniversalTime();
                var endUtc = end.ToUniversalTime();

                //TODO: don't use EF directly here
                var manualQuery = Db.CalendarEvents
                    .Include(e => e.Invites)
                    .Where(e => e.StartUtc < endUtc && e.EndUtc > startUtc)
                    .Where(e => e.CreatedByUserId == user!.Id || (e.Invites != null && e.Invites.Any(i => i.InvitedUserId == user!.Id)));

                if (categoryId != null)
                {
                    manualQuery = manualQuery.Where(e => e.CategoryValueId == categoryId.Value);
                }

                var manualEntities = await manualQuery.OrderBy(e => e.StartUtc).ToListAsync(ct);

                var accessibleCaseIds = await GetAccessibleCaseIdsAsync(user!, ct);
                var caseEvents = categoryId == null
                    ? await ProjectCaseEventsAsync(startUtc, endUtc, accessibleCaseIds, ct)
                    : new List<CalendarEventResponseModel>();

                var categoryNamesById = await GetCategoryNameLookupAsync(ct);
                var involvedUserIds = manualEntities
                    .SelectMany(e => (e.Invites ?? new List<CalendarEventInviteEntityModel>())
                        .Select(i => (Guid?)i.InvitedUserId)
                        .Append(e.CreatedByUserId)
                        .Where(id => id.HasValue)
                        .Select(id => id!.Value))
                    .Distinct();
                var userNamesById = await GetUserNameLookupAsync(involvedUserIds, ct);

                var manualResponses = manualEntities
                    .Select(e => CalendarEventResponseModel.FromEntity(e, user!.Id, categoryNamesById, userNamesById));

                var response = manualResponses.Concat(caseEvents)
                    .OrderBy(e => e.Start)
                    .ToList();

                return StatusCode(StatusCodes.Status200OK, response);
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex, "Failed to fetch calendar events");
                return StatusCode(StatusCodes.Status500InternalServerError, new FailureResponseModel { Detail = "Failed to fetch calendar events" });
            }
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(CalendarEventResponseModel), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<CalendarEventResponseModel>> GetEventById(Guid id, CancellationToken ct = default)
        {
            try
            {
                var (user, error) = await RequireCalendarAccessAsync();
                if (error != null) return error;

                var entity = await Db.CalendarEvents.Include(e => e.Invites).FirstOrDefaultAsync(e => e.Id == id, ct);
                if (entity == null)
                {
                    return StatusCode(StatusCodes.Status404NotFound, new FailureResponseModel { Detail = "Calendar event not found" });
                }

                var isInvited = entity.Invites?.Any(i => i.InvitedUserId == user!.Id) ?? false;
                var canView = user!.IsAdministrator || entity.CreatedByUserId == user.Id || isInvited;
                if (!canView)
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new FailureResponseModel { Detail = "You don't have permission to view this event" });
                }

                var categoryNamesById = await GetCategoryNameLookupAsync(ct);
                var involvedUserIds = (entity.Invites ?? new List<CalendarEventInviteEntityModel>())
                    .Select(i => (Guid?)i.InvitedUserId)
                    .Append(entity.CreatedByUserId)
                    .Where(id => id.HasValue)
                    .Select(id => id!.Value);
                var userNamesById = await GetUserNameLookupAsync(involvedUserIds, ct);

                return StatusCode(StatusCodes.Status200OK, CalendarEventResponseModel.FromEntity(entity, user.Id, categoryNamesById, userNamesById));
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex, "Failed to fetch calendar event {EventId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, new FailureResponseModel { Detail = "Failed to fetch calendar event" });
            }
        }

        [HttpPost("")]
        [ProducesResponseType(typeof(CalendarEventResponseModel), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<CalendarEventResponseModel>> CreateEvent(
            [FromBody] CalendarEventCreationRequestModel request,
            CancellationToken ct = default)
        {
            try
            {
                var (user, error) = await RequireCalendarAccessAsync();
                if (error != null) return error;

                if (request.End <= request.Start)
                {
                    return StatusCode(StatusCodes.Status400BadRequest, new FailureResponseModel { Detail = "end must be after start" });
                }

                var categoryError = await ValidateCategoryAsync(request.CategoryId, ct);
                if (categoryError != null) return categoryError;

                List<Guid> validInviteeIds = new();
                if (request.InviteeUserIds is { Count: > 0 })
                {
                    var inviteeError = await ValidateUserIdsAsync(request.InviteeUserIds, ct);
                    if (inviteeError != null) return inviteeError;
                    validInviteeIds = request.InviteeUserIds.Distinct().Where(id => id != user!.Id).ToList();
                }

                var entity = new CalendarEventEntityModel
                {
                    Id = Guid.NewGuid(),
                    Title = request.Title,
                    StartUtc = request.Start.ToUniversalTime(),
                    EndUtc = request.End.ToUniversalTime(),
                    IsAllDay = request.AllDay,
                    CategoryValueId = request.CategoryId,
                    CaseId = request.CaseId,
                    Location = request.Location,
                    Description = request.Description,
                    CreatedByUserId = user!.Id,
                    CreatedAtUtc = DateTime.UtcNow,
                    LastUpdatedAtUtc = DateTime.UtcNow,
                    LastUpdatedByUserId = user.Id,
                    Invites = validInviteeIds.Select(inviteeId => new CalendarEventInviteEntityModel
                    {
                        Id = Guid.NewGuid(),
                        CreatedAtUtc = DateTime.UtcNow,
                        CreatedByUserId = user.Id,
                        InvitedUserId = inviteeId,
                        Status = CalendarEventInviteStatusEnum.Pending,
                        InvitedByUserId = user.Id,
                        InvitedAtUtc = DateTime.UtcNow
                    }).ToList()
                };

                //TODO: don't use EF directly here
                Db.CalendarEvents.Add(entity);
                await Db.SaveChangesAsync(ct);

                this.Logger.LogInformation(
                    "Created calendar event {EventId} (category {CategoryId}, {InviteeCount} invitee(s)) by user {UserId}",
                    entity.Id, entity.CategoryValueId, validInviteeIds.Count, user.Id);

                var categoryNamesById = await GetCategoryNameLookupAsync(ct);
                var userNamesById = await GetUserNameLookupAsync(validInviteeIds.Append(user.Id), ct);
                return StatusCode(StatusCodes.Status201Created, CalendarEventResponseModel.FromEntity(entity, user.Id, categoryNamesById, userNamesById));
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex, "Failed to create calendar event");
                return StatusCode(StatusCodes.Status500InternalServerError, new FailureResponseModel { Detail = "Failed to create calendar event" });
            }
        }

        [HttpPatch("{id:guid}")]
        [ProducesResponseType(typeof(CalendarEventResponseModel), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<CalendarEventResponseModel>> UpdateEvent(
            Guid id,
            [FromBody] CalendarEventUpdateRequestModel request,
            CancellationToken ct = default)
        {
            try
            {
                var (user, error) = await RequireCalendarAccessAsync();
                if (error != null) return error;

                var entity = await Db.CalendarEvents.Include(e => e.Invites).FirstOrDefaultAsync(e => e.Id == id, ct);
                if (entity == null)
                {
                    return StatusCode(StatusCodes.Status404NotFound, new FailureResponseModel { Detail = "Calendar event not found" });
                }

                var canUpdate = HasCalendarAccess(user!) || entity.CreatedByUserId == user.Id;
                if (!canUpdate)
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new FailureResponseModel { Detail = "You don't have permission to update this event" });
                }

                var newStart = request.Start ?? entity.StartUtc;
                var newEnd = request.End ?? entity.EndUtc;
                if (newEnd <= newStart)
                {
                    return StatusCode(StatusCodes.Status400BadRequest, new FailureResponseModel { Detail = "end must be after start" });
                }

                if (request.CategoryId != null)
                {
                    var categoryError = await ValidateCategoryAsync(request.CategoryId.Value, ct);
                    if (categoryError != null) return categoryError;
                    entity.CategoryValueId = request.CategoryId.Value;
                }

                if (request.Title != null)
                    entity.Title = request.Title;

                if (request.Start != null)
                    entity.StartUtc = request.Start.Value.ToUniversalTime();

                if (request.End != null)
                    entity.EndUtc = request.End.Value.ToUniversalTime();

                if (request.AllDay != null)
                    entity.IsAllDay = request.AllDay.Value;

                if (request.CaseId != null)
                    entity.CaseId = request.CaseId;

                if (request.Location != null)
                    entity.Location = request.Location;

                if (request.Description != null)
                    entity.Description = request.Description;

                entity.LastUpdatedAtUtc = DateTime.UtcNow;
                entity.LastUpdatedByUserId = user.Id;

                await Db.SaveChangesAsync(ct);

                this.Logger.LogInformation("Updated calendar event {EventId} by user {UserId}", entity.Id, user.Id);

                var categoryNamesById = await GetCategoryNameLookupAsync(ct);
                var involvedUserIds = (entity.Invites ?? new List<CalendarEventInviteEntityModel>())
                    .Select(i => (Guid?)i.InvitedUserId)
                    .Append(entity.CreatedByUserId)
                    .Where(id => id.HasValue)
                    .Select(id => id!.Value);
                var userNamesById = await GetUserNameLookupAsync(involvedUserIds, ct);
                return StatusCode(StatusCodes.Status200OK, CalendarEventResponseModel.FromEntity(entity, user.Id, categoryNamesById, userNamesById));
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex, "Failed to update calendar event {EventId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, new FailureResponseModel { Detail = "Failed to update calendar event" });
            }
        }

        [HttpDelete("{id:guid}")]
        [ProducesResponseType(typeof(SuccessResponseModel), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<SuccessResponseModel>> DeleteEvent(Guid id, CancellationToken ct = default)
        {
            try
            {
                var (user, error) = await RequireCalendarAccessAsync();
                if (error != null) return error;

                var entity = await Db.CalendarEvents.FirstOrDefaultAsync(e => e.Id == id, ct);
                if (entity == null)
                {
                    return StatusCode(StatusCodes.Status404NotFound, new FailureResponseModel { Detail = "Calendar event not found" });
                }

                var canDelete = HasCalendarAccess(user!) || entity.CreatedByUserId == user.Id;
                if (!canDelete)
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new FailureResponseModel
                    {
                        Detail = "Only case workers, admins, or the event's creator can delete it"
                    });
                }

                Db.CalendarEvents.Remove(entity);
                await Db.SaveChangesAsync(ct);

                this.Logger.LogInformation("Deleted calendar event {EventId} by user {UserId}", id, user.Id);

                return StatusCode(StatusCodes.Status200OK, new SuccessResponseModel());
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex, "Failed to delete calendar event {EventId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, new FailureResponseModel { Detail = "Failed to delete calendar event" });
            }
        }

        #endregion
        #region Sharing

        [HttpPost("{id:guid}/invites")]
        [ProducesResponseType(typeof(CalendarEventResponseModel), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<CalendarEventResponseModel>> InvitePeople(
            Guid id,
            [FromBody] CalendarEventInviteCreateRequestModel request,
            CancellationToken ct = default)
        {
            try
            {
                var (user, error) = await RequireCalendarAccessAsync();
                if (error != null) return error;

                var entity = await Db.CalendarEvents.Include(e => e.Invites).FirstOrDefaultAsync(e => e.Id == id, ct);
                if (entity == null)
                {
                    return StatusCode(StatusCodes.Status404NotFound, new FailureResponseModel { Detail = "Calendar event not found" });
                }

                if (!user!.IsAdministrator && !user.IsCaseWorkerManager && entity.CreatedByUserId != user.Id)
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new FailureResponseModel { Detail = "Only the event's creator (or an admin) can invite people to it" });
                }

                var userIdsError = await ValidateUserIdsAsync(request.UserIds, ct);
                if (userIdsError != null) return userIdsError;

                var existingInviteeIds = (entity.Invites ?? new List<CalendarEventInviteEntityModel>()).Select(i => i.InvitedUserId).ToHashSet();
                var newInvitees = request.UserIds.Distinct()
                    .Where(uid => uid != entity.CreatedByUserId && !existingInviteeIds.Contains(uid))
                    .ToList();

                entity.Invites ??= new List<CalendarEventInviteEntityModel>();
                foreach (var inviteeId in newInvitees)
                {
                    entity.Invites.Add(new CalendarEventInviteEntityModel
                    {
                        Id = Guid.NewGuid(),
                        CreatedAtUtc = DateTime.UtcNow,
                        CreatedByUserId = user.Id,
                        CalendarEventId = entity.Id,
                        InvitedUserId = inviteeId,
                        Status = CalendarEventInviteStatusEnum.Pending,
                        InvitedByUserId = user.Id,
                        InvitedAtUtc = DateTime.UtcNow
                    });
                }

                await Db.SaveChangesAsync(ct);

                this.Logger.LogInformation("Invited {Count} user(s) to calendar event {EventId} by user {UserId}", newInvitees.Count, id, user.Id);

                var categoryNamesById = await GetCategoryNameLookupAsync(ct);
                var involvedUserIds = entity.Invites
                    .Select(i => (Guid?)i.InvitedUserId)
                    .Append(entity.CreatedByUserId)
                    .Where(id => id.HasValue)
                    .Select(id => id!.Value);
                var userNamesById = await GetUserNameLookupAsync(involvedUserIds, ct);
                return StatusCode(StatusCodes.Status200OK, CalendarEventResponseModel.FromEntity(entity, user.Id, categoryNamesById, userNamesById));
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex, "Failed to invite people to calendar event {EventId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, new FailureResponseModel { Detail = "Failed to invite people" });
            }
        }

        [HttpPatch("{id:guid}/invites/me")]
        [ProducesResponseType(typeof(CalendarEventResponseModel), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<CalendarEventResponseModel>> RespondToInvite(
            Guid id,
            [FromBody] CalendarEventInviteRespondRequestModel request,
            CancellationToken ct = default)
        {
            try
            {
                var (user, error) = await RequireCalendarAccessAsync();
                if (error != null) return error;

                var statusValue = request.Status.Trim().ToLowerInvariant();
                if (statusValue != "accepted" && statusValue != "declined")
                {
                    return StatusCode(StatusCodes.Status400BadRequest, new FailureResponseModel { Detail = "status must be 'accepted' or 'declined'" });
                }

                var entity = await Db.CalendarEvents.Include(e => e.Invites).FirstOrDefaultAsync(e => e.Id == id, ct);
                if (entity == null)
                {
                    return StatusCode(StatusCodes.Status404NotFound, new FailureResponseModel { Detail = "Calendar event not found" });
                }

                var myInvite = entity.Invites?.FirstOrDefault(i => i.InvitedUserId == user!.Id);
                if (myInvite == null)
                {
                    return StatusCode(StatusCodes.Status404NotFound, new FailureResponseModel { Detail = "You don't have an invite for this event" });
                }

                myInvite.Status = statusValue == "accepted" ? CalendarEventInviteStatusEnum.Accepted : CalendarEventInviteStatusEnum.Declined;
                myInvite.RespondedAtUtc = DateTime.UtcNow;

                await Db.SaveChangesAsync(ct);

                this.Logger.LogInformation("User {UserId} {Status} invite to calendar event {EventId}", user!.Id, statusValue, id);

                var categoryNamesById = await GetCategoryNameLookupAsync(ct);
                var involvedUserIds = (entity.Invites ?? new List<CalendarEventInviteEntityModel>())
                    .Select(i => (Guid?)i.InvitedUserId)
                    .Append(entity.CreatedByUserId)
                    .Where(id => id.HasValue)
                    .Select(id => id!.Value);
                var userNamesById = await GetUserNameLookupAsync(involvedUserIds, ct);
                return StatusCode(StatusCodes.Status200OK, CalendarEventResponseModel.FromEntity(entity, user.Id, categoryNamesById, userNamesById));
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex, "Failed to respond to invite for calendar event {EventId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, new FailureResponseModel { Detail = "Failed to respond to invite" });
            }
        }

        [HttpDelete("{id:guid}/invites/{userId:guid}")]
        [ProducesResponseType(typeof(SuccessResponseModel), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<SuccessResponseModel>> RemoveInvite(Guid id, Guid userId, CancellationToken ct = default)
        {
            try
            {
                var (user, error) = await RequireCalendarAccessAsync();
                if (error != null) return error;

                var entity = await Db.CalendarEvents.Include(e => e.Invites).FirstOrDefaultAsync(e => e.Id == id, ct);
                if (entity == null)
                {
                    return StatusCode(StatusCodes.Status404NotFound, new FailureResponseModel { Detail = "Calendar event not found" });
                }

                if (!user!.IsAdministrator && !user.IsCaseWorkerManager && entity.CreatedByUserId != user.Id && userId != user.Id)
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new FailureResponseModel { Detail = "You don't have permission to remove this invite" });
                }

                var invite = entity.Invites?.FirstOrDefault(i => i.InvitedUserId == userId);
                if (invite == null)
                {
                    return StatusCode(StatusCodes.Status404NotFound, new FailureResponseModel { Detail = "Invite not found" });
                }

                Db.CalendarEventInvites.Remove(invite);
                await Db.SaveChangesAsync(ct);

                this.Logger.LogInformation("Removed invite for user {InvitedUserId} from calendar event {EventId} by user {UserId}", userId, id, user.Id);

                return StatusCode(StatusCodes.Status200OK, new SuccessResponseModel());
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex, "Failed to remove invite from calendar event {EventId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, new FailureResponseModel { Detail = "Failed to remove invite" });
            }
        }

        #endregion
        #region Categories

        [HttpGet("categories")]
        [ProducesResponseType(typeof(List<CalendarEventCategoryResponseModel>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<CalendarEventCategoryResponseModel>>> GetCategories(
            [FromQuery] bool includeInactive = false,
            CancellationToken ct = default)
        {
            try
            {
                var (user, error) = await RequireCalendarAccessAsync();
                if (error != null) return error;

                if (includeInactive && !user!.IsAdministrator)
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new FailureResponseModel { Detail = "Only administrators can view inactive categories." });
                }

                var enumerator = await _dataEnumeratorService.GetEnumeratorByNameAsync(
                    Constants.CalendarEventCategoryDataEnumeratorName,
                    activeValuesOnly: !includeInactive,
                    ct
                );

                if (enumerator?.EnumeratorValues == null)
                {
                    return StatusCode(StatusCodes.Status200OK, new List<CalendarEventCategoryResponseModel>());
                }

                var response = enumerator.EnumeratorValues
                    .OrderBy(v => v.SortOrder)
                    .Select(v => new CalendarEventCategoryResponseModel
                    {
                        Id = v.Id,
                        Name = v.CanonicalName,
                        IsActive = v.IsActive,
                        SortOrder = v.SortOrder,
                        Colour = v.ColourHex ?? Constants.CalendarEventCategoryDataEnumeratorValueDefaultColourHexValue
                    })
                    .ToList();

                return StatusCode(StatusCodes.Status200OK, response);
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex, "Failed to fetch calendar event categories");
                return StatusCode(StatusCodes.Status500InternalServerError, new FailureResponseModel { Detail = "Failed to fetch categories" });
            }
        }

        [HttpPatch("categories/{valueId:guid}/colour")]
        [ProducesResponseType(typeof(CalendarEventCategoryResponseModel), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<CalendarEventCategoryResponseModel>> SetCategoryColour(
            Guid valueId,
            [FromBody] CalendarEventCategoryColourUpdateRequestModel request,
            CancellationToken ct = default)
        {
            try
            {
                var adminError = await RequireAdminAsync();
                if (adminError != null) return adminError;

                var (user, error) = await GetCurrentUserAsync();
                if (error != null) return error;

                if (!System.Text.RegularExpressions.Regex.IsMatch(request.Colour, "^#[0-9A-Fa-f]{6}$"))
                {
                    return StatusCode(StatusCodes.Status400BadRequest, new FailureResponseModel { Detail = "colour must be a 6-digit hex value, e.g. #2F6F65" });
                }

                var enumerator = await _dataEnumeratorService.GetEnumeratorByNameAsync(
                    Constants.CalendarEventCategoryDataEnumeratorName,
                    activeValuesOnly: false,
                    ct
                );
                var belongsToOurEnumerator = enumerator?.EnumeratorValues?.Any(v => v.Id == valueId) ?? false;
                if (!belongsToOurEnumerator)
                {
                    return StatusCode(StatusCodes.Status404NotFound, new FailureResponseModel { Detail = "Category not found" });
                }

                var value = await _dataEnumeratorService.GetValueAsync(valueId, ct);
                if (value == null)
                {
                    return StatusCode(StatusCodes.Status404NotFound, new FailureResponseModel { Detail = "Category not found" });
                }

                value.ColourHex = request.Colour;
                value.LastUpdatedAtUtc = DateTime.UtcNow;
                value.LastUpdatedByUserId = user!.Id;

                await Db.SaveChangesAsync(ct);

                this.Logger.LogInformation("Set colour for calendar category {CategoryId} to {Colour} by user {UserId}", valueId, request.Colour, user.Id);

                return StatusCode(StatusCodes.Status200OK, new CalendarEventCategoryResponseModel
                {
                    Id = value.Id,
                    Name = value.CanonicalName,
                    IsActive = value.IsActive,
                    SortOrder = value.SortOrder,
                    Colour = value.ColourHex
                });
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex, "Failed to set colour for calendar category {CategoryId}", valueId);
                return StatusCode(StatusCodes.Status500InternalServerError, new FailureResponseModel { Detail = "Failed to set category colour" });
            }
        }

        #endregion
        #region Helpers

        private static bool HasCalendarAccess(UserEntityModel user) =>
            user.IsAdministrator || user.IsCaseWorker || user.IsCaseWorkerManager;

        private async Task<(UserEntityModel? user, ActionResult? error)> RequireCalendarAccessAsync()
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return (null, error);

            if (!HasCalendarAccess(user!))
            {
                return (null, StatusCode(StatusCodes.Status403Forbidden, new FailureResponseModel
                {
                    Detail = "Only case workers, case worker managers, and administrators can access the calendar."
                }));
            }

            return (user, null);
        }

        private async Task<ActionResult?> ValidateCategoryAsync(Guid categoryId, CancellationToken ct)
        {
            var enumerator = await _dataEnumeratorService.GetEnumeratorByNameAsync(
                Constants.CalendarEventCategoryDataEnumeratorName,
                activeValuesOnly: false,
                ct
            );

            var value = enumerator?.EnumeratorValues?.FirstOrDefault(v => v.Id == categoryId);
            if (value == null)
            {
                return StatusCode(StatusCodes.Status400BadRequest, new FailureResponseModel { Detail = "Unknown categoryId" });
            }

            if (!value.IsActive)
            {
                return StatusCode(StatusCodes.Status400BadRequest, new FailureResponseModel { Detail = "That category is inactive" });
            }

            return null;
        }

        private async Task<ActionResult?> ValidateUserIdsAsync(List<Guid> userIds, CancellationToken ct)
        {
            var distinctIds = userIds.Distinct().ToList();
            var users = await Db.Users.Where(u => distinctIds.Contains(u.Id)).ToListAsync(ct);

            if (users.Count != distinctIds.Count)
            {
                return StatusCode(StatusCodes.Status400BadRequest, new FailureResponseModel { Detail = "One or more userIds don't exist" });
            }

            if (users.Any(u => !HasCalendarAccess(u)))
            {
                return StatusCode(StatusCodes.Status400BadRequest, new FailureResponseModel
                {
                    Detail = "Can only invite case workers, case worker managers, or administrators — they're the only users with calendar access."
                });
            }

            return null;
        }

        private async Task<Dictionary<Guid, string>> GetCategoryNameLookupAsync(CancellationToken ct)
        {
            var enumerator = await _dataEnumeratorService.GetEnumeratorByNameAsync(
                Constants.CalendarEventCategoryDataEnumeratorName,
                activeValuesOnly: false,
                ct
            );

            return enumerator?.EnumeratorValues?.ToDictionary(v => v.Id, v => v.CanonicalName)
                ?? new Dictionary<Guid, string>();
        }

        private async Task<Dictionary<Guid, string>> GetUserNameLookupAsync(IEnumerable<Guid> userIds, CancellationToken ct)
        {
            var distinctIds = userIds.Distinct().ToList();
            if (distinctIds.Count == 0) return new Dictionary<Guid, string>();

            return await Db.Users
                .Where(u => distinctIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.FullName, ct);
        }

        private async Task<HashSet<Guid>> GetAccessibleCaseIdsAsync(UserEntityModel user, CancellationToken ct)
        {
            if (user.IsAdministrator)
            {
                return (await Db.Cases.Select(c => c.Id).ToListAsync(ct)).ToHashSet();
            }

            var asClient = Db.CaseClients.Where(cc => cc.UserId == user.Id).Select(cc => cc.CaseId);
            var asWorker = Db.CaseWorkers.Where(cw => cw.UserId == user.Id).Select(cw => cw.CaseId);

            return (await asClient.Union(asWorker).ToListAsync(ct)).ToHashSet();
        }

        private async Task<List<CalendarEventResponseModel>> ProjectCaseEventsAsync(
            DateTime startUtc,
            DateTime endUtc,
            HashSet<Guid> accessibleCaseIds,
            CancellationToken ct)
        {
            if (accessibleCaseIds.Count == 0) return new List<CalendarEventResponseModel>();

            var results = new List<CalendarEventResponseModel>();

            var dueTodos = await Db.CaseTodos
                .Where(t => accessibleCaseIds.Contains(t.CaseId) && t.DueDate != null && t.DueDate >= startUtc && t.DueDate < endUtc)
                .ToListAsync(ct);
            foreach (var todo in dueTodos)
            {
                var due = todo.DueDate!.Value;
                results.Add(new CalendarEventResponseModel
                {
                    Id = $"todo-due:{todo.Id}",
                    Source = "case",
                    SourceType = "todo_due",
                    Title = $"Due: {todo.Summary}",
                    Start = due.Date,
                    End = due.Date.AddDays(1),
                    AllDay = true,
                    CaseId = todo.CaseId,
                    IsOwner = false,
                    IsShareable = false,
                });
            }

            var reminderTodos = await Db.CaseTodos
                .Where(t => accessibleCaseIds.Contains(t.CaseId) && t.ReminderUtc != null && t.ReminderUtc >= startUtc && t.ReminderUtc < endUtc)
                .ToListAsync(ct);
            foreach (var todo in reminderTodos)
            {
                var reminder = todo.ReminderUtc!.Value;
                results.Add(new CalendarEventResponseModel
                {
                    Id = $"todo-reminder:{todo.Id}",
                    Source = "case",
                    SourceType = "todo_reminder",
                    Title = $"Reminder: {todo.Summary}",
                    Start = reminder,
                    End = reminder.AddMinutes(15),
                    AllDay = false,
                    CaseId = todo.CaseId,
                    IsOwner = false,
                    IsShareable = false,
                });
            }

            var timelineEntries = await Db.CaseTimelineEntries
                .Where(e => accessibleCaseIds.Contains(e.CaseId) && e.OccurredAtUtc >= startUtc && e.OccurredAtUtc < endUtc)
                .ToListAsync(ct);
            foreach (var entry in timelineEntries)
            {
                results.Add(new CalendarEventResponseModel
                {
                    Id = $"timeline:{entry.Id}",
                    Source = "case",
                    SourceType = "case_timeline",
                    Title = entry.Title,
                    Description = entry.Description,
                    Start = entry.OccurredAtUtc.Date,
                    End = entry.OccurredAtUtc.Date.AddDays(1),
                    AllDay = true,
                    CaseId = entry.CaseId,
                    IsOwner = false,
                    IsShareable = false,
                });
            }

            return results;
        }
        #endregion
    }
}
