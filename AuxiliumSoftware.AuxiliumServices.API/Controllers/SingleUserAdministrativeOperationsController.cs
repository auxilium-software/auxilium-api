using AuxiliumSoftware.AuxiliumServices.API.Common.ControllerBases;
using AuxiliumSoftware.AuxiliumServices.API.Common.Utilities;
using AuxiliumSoftware.AuxiliumServices.API.Models;
using AuxiliumSoftware.AuxiliumServices.API.Models.User;
using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework;
using AuxiliumSoftware.AuxiliumServices.Common.Messaging.Interfaces;
using AuxiliumSoftware.AuxiliumServices.Common.Messaging.Models;
using AuxiliumSoftware.AuxiliumServices.Common.Messaging.Models.Enumerators;
using AuxiliumSoftware.AuxiliumServices.Common.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuxiliumSoftware.AuxiliumServices.API.Controllers
{
    [ApiController]
    [Route("/api/v3/users/{userId:guid}")]
    [Tags("Users")]
    public class SingleUserAdministrativeOperationsController : LoggedInControllerBase
    {
        private readonly IUserDocumentService _userDocService;
        private readonly IMessageQueueProducer _messageQueueProducer;

        public SingleUserAdministrativeOperationsController(
            ISystemSettingsService systemSettingsService,
            IConfiguration configuration,
            AuxiliumDbContext db,
            IWebApplicationFirewallService waf,
            ILogger<SingleUserController> logger,
            ITotpService totpService,

            IUserDocumentService userDocService,
            IMessageQueueProducer messageQueueProducer
            )
            : base(systemSettingsService, configuration, db, waf, logger, totpService)
        {
            _userDocService = userDocService;
            _messageQueueProducer = messageQueueProducer;
        }


        [HttpPatch("permissions")]
        [ProducesResponseType(typeof(UserResponseModel), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<UserResponseModel>> UpdatePermissions(
        Guid userId,
        [FromBody] UpdatePermissionsRequestModel request
    )
        {
            try
            {
                var (user, error) = await GetCurrentUserAsync();
                if (error != null) return error;

                var adminError = await RequireAdminAsync();
                if (adminError != null) return adminError;

                var totpError = await RequireTotpAsync(user!.Id);
                if (totpError != null) return totpError;

                var userDoc = await Db.Users
                    .Include(u => u.AdditionalProperties)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (userDoc == null)
                {
                    return NotFound(new FailureResponseModel { Detail = "User not found" });
                }

                // Prevent admins from removing their own admin rights
                if (userId == user!.Id && request.IsAdministrator == false && userDoc.IsAdministrator)
                {
                    return BadRequest(new FailureResponseModel
                    {
                        Detail = "You cannot remove your own administrator privileges"
                    });
                }

                var changes = new List<string>();

                if (request.IsAdministrator.HasValue && userDoc.IsAdministrator != request.IsAdministrator.Value)
                {
                    userDoc.IsAdministrator = request.IsAdministrator.Value;
                    changes.Add(request.IsAdministrator.Value ? "Granted admin" : "Revoked admin");
                }

                if (request.IsCaseWorkerManager.HasValue && userDoc.IsCaseWorkerManager != request.IsCaseWorkerManager.Value)
                {
                    userDoc.IsCaseWorkerManager = request.IsCaseWorkerManager.Value;
                    changes.Add(request.IsCaseWorkerManager.Value ? "Granted case worker manager" : "Revoked case worker manager");
                }

                if (request.IsCaseWorker.HasValue && userDoc.IsCaseWorker != request.IsCaseWorker.Value)
                {
                    userDoc.IsCaseWorker = request.IsCaseWorker.Value;
                    changes.Add(request.IsCaseWorker.Value ? "Granted case worker" : "Revoked case worker");
                }

                if (changes.Count > 0)
                {
                    userDoc.LastUpdatedAt = DateTime.UtcNow;
                    userDoc.LastUpdatedBy = user.Id;

                    await Db.SaveChangesAsync();

                    Logger.LogInformation(
                        "Permissions updated for user {UserId} by admin {AdminId}: {Changes}",
                        userId, user.Id, string.Join(", ", changes)
                    );
                }

                return Ok(ControllerUtilities.UserMapToUserResponseModel(userDoc, true));
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to update permissions for user {UserId}", userId);
                return StatusCode(500, new FailureResponseModel
                {
                    Detail = "Failed to update permissions"
                });
            }
        }

        /*
        [HttpPost("block")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> SetUserBlocked(
            Guid userId,
            [FromBody] SetBlockedRequestModel request
        )
        {
            try
            {
                var (user, error) = await GetCurrentUserAsync();
                if (error != null) return error;

                await this.RequireAdminAsync();

                var totpError = await RequireTotpAsync(user!.Id);
                if (totpError != null) return totpError;

                var userDoc = await Db.Users.FirstOrDefaultAsync(u => u.Id == userId);

                if (userDoc == null)
                {
                    return NotFound(new FailureResponseModel { Detail = "User not found" });
                }

                // Prevent admins from blocking themselves
                if (userId == user!.Id)
                {
                    return BadRequest(new FailureResponseModel
                    {
                        Detail = "You cannot block your own account"
                    });
                }

                userDoc.AllowLogin = !request.Blocked;
                userDoc.LastUpdatedAt = DateTime.UtcNow;
                userDoc.LastUpdatedBy = user.Id;

                await Db.SaveChangesAsync();

                var action = request.Blocked ? "user.blocked" : "user.unblocked";
                var desc = request.Blocked
                    ? "Login blocked - user can no longer sign in"
                    : "Login unblocked - user can sign in again";

                await WriteAuditLog(userId, user.Id, action, desc);

                Logger.LogInformation(
                    "User {UserId} {Action} by admin {AdminId}",
                    userId, request.Blocked ? "blocked" : "unblocked", user.Id
                );

                return Ok(new { blocked = request.Blocked, allowLogin = userDoc.AllowLogin });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to update block status for user {UserId}", userId);
                return StatusCode(500, new FailureResponseModel
                {
                    Detail = "Failed to update block status"
                });
            }
        }
        */

        /*
        [HttpGet("audit-log")]
        [ProducesResponseType(typeof(List<AuditLogEntryModel>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<AuditLogEntryModel>>> GetAuditLog(
            Guid userId,
            [FromQuery] int limit = 50
        )
        {
            try
            {
                var (user, error) = await GetCurrentUserAsync();
                if (error != null) return error;
                var adminError = await this.RequireAdminAsync();
                if (adminError != null) return adminError;

                var entries = await Db.AuditLogs
                    .Where(a => a.TargetUserId == userId)
                    .OrderByDescending(a => a.Timestamp)
                    .Take(limit)
                    .Select(a => new AuditLogEntryModel
                    {
                        Id = a.Id,
                        Timestamp = a.Timestamp,
                        Action = a.Action,
                        Description = a.Description,
                        Actor = a.ActorName
                    })
                    .ToListAsync();

                return Ok(entries);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to fetch audit log for user {UserId}", userId);
                return StatusCode(500, new FailureResponseModel
                {
                    Detail = "Failed to fetch audit log"
                });
            }
        }
        */

        [HttpPost("force-password-reset")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> ForcePasswordReset(Guid userId)
        {
            try
            {
                var (user, error) = await GetCurrentUserAsync();
                if (error != null) return error;

                var adminError = await RequireAdminAsync();
                if (adminError != null) return adminError;

                var totpError = await RequireTotpAsync(user!.Id);
                if (totpError != null) return totpError;

                var userDoc = await Db.Users.FirstOrDefaultAsync(u => u.Id == userId);

                if (userDoc == null)
                {
                    return NotFound(new FailureResponseModel { Detail = "User not found" });
                }

                
                if (userDoc.MustChangePassword)
                {
                    return Ok(new { success = true, message = "Password reset already pending" });
                }
                

                userDoc.MustChangePassword = true;
                userDoc.LastUpdatedAt = DateTime.UtcNow;
                userDoc.LastUpdatedBy = user!.Id;

                await _messageQueueProducer.PublishAsync(new EmailQueueMessage
                {
                    To = userDoc.EmailAddress,
                    Subject = "Password Change Required",
                    TemplateName = "force-password-reset",
                    Priority = EmailPriorityEnum.High,
                    TemplateData = new Dictionary<string, string>
                        {
                            { "displayName", userDoc.FullName },
                            { "userId", userDoc.Id.ToString() }
                        }
                });

                await Db.SaveChangesAsync();

                Logger.LogInformation(
                    "Password reset forced for user {UserId} by admin {AdminId}",
                    userId, user.Id
                );

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to force password reset for user {UserId}", userId);
                return StatusCode(500, new FailureResponseModel
                {
                    Detail = "Failed to force password reset"
                });
            }
        }
    }
}
