using AuxiliumSoftware.AuxiliumServices.API.Common.ControllerBases;
using AuxiliumSoftware.AuxiliumServices.API.Models;
using AuxiliumSoftware.AuxiliumServices.API.Models.SystemBulletin;
using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework;
using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework.EntityModels;
using AuxiliumSoftware.AuxiliumServices.Common.Enumerators;
using AuxiliumSoftware.AuxiliumServices.Common.Services;
using AuxiliumSoftware.AuxiliumServices.Common.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AuxiliumSoftware.AuxiliumServices.API.Controllers
{
    [ApiController]
    [Route("/api/v3/system-bulletin")]
    [Tags("System Bulletin")]
    [Authorize]
    public class SystemBulletinAdminController : LoggedInControllerBase
    {
        public SystemBulletinAdminController(
            ISystemSettingsService systemSettingsService,
            IConfiguration configuration,
            AuxiliumDbContext db,
            IWebApplicationFirewallService waf,
            ILogger<SystemBulletinAdminController> logger,
            ITotpService totpService
        )
        : base(systemSettingsService, configuration, db, waf, logger, totpService)
        {
        }



        [HttpGet("all")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<List<SystemBulletinAdminResponseModel>>> GetAllMessages()
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            var adminError = await RequireAdminAsync();
            if (adminError != null) return adminError;

            var query = this.Db.System_Bulletins;

            var bulletins = await query
                .OrderByDescending(b => b.Severity)
                .ThenByDescending(b => b.CreatedAtUtc)
                .Select(b => new SystemBulletinAdminResponseModel
                {
                    Id = b.Id,
                    CreatedAt = b.CreatedAtUtc,
                    CreatedBy = b.CreatedBy,
                    Severity = b.Severity,
                    Title = b.Title,
                    Content = b.Content,
                    IsActive = b.IsActive,
                    IsDismissible = b.IsDismissible,
                    StartsAt = b.StartsAtUtc,
                    EndsAt = b.EndsAtUtc,
                    TargetAudience = b.TargetAudience,
                    SpecificUserId = b.SpecificUserId
                })
                .ToListAsync();

            return StatusCode(StatusCodes.Status200OK, bulletins);
        }

        [HttpPost("")]
        [ProducesResponseType(typeof(SystemBulletinAdminResponseModel), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<SystemBulletinAdminResponseModel>> CreateBulletin(
            [FromBody] SystemBulletinCreationRequestModel request)
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            var adminError = await RequireAdminAsync();
            if (adminError != null) return adminError;


            var bulletin = new SystemBulletinEntryEntityModel
            {
                Id = Guid.NewGuid(),
                CreatedAtUtc = DateTime.UtcNow,
                CreatedBy = user.Id,
                Severity = request.Severity,
                Title = request.Title,
                Content = request.Content,
                IsDismissible = request.IsDismissible,
                IsActive = true,
                StartsAtUtc = request.StartsAt ?? DateTime.UtcNow,
                EndsAtUtc = request.EndsAt,
                TargetAudience = request.TargetAudience,
                SpecificUserId = request.SpecificUserId
            };

            this.Db.System_Bulletins.Add(bulletin);
            await this.Db.SaveChangesAsync();

            return StatusCode(StatusCodes.Status201Created, new SystemBulletinAdminResponseModel
            {
                Id = bulletin.Id,
                CreatedAt = bulletin.CreatedAtUtc,
                CreatedBy = bulletin.CreatedBy,
                Severity = bulletin.Severity,
                Title = bulletin.Title,
                Content = bulletin.Content,
                IsActive = bulletin.IsActive,
                IsDismissible = bulletin.IsDismissible,
                StartsAt = bulletin.StartsAtUtc,
                EndsAt = bulletin.EndsAtUtc,
                TargetAudience = bulletin.TargetAudience,
                SpecificUserId = bulletin.SpecificUserId
            });
        }

        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DeleteBulletin(Guid id)
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            var adminError = await RequireAdminAsync();
            if (adminError != null) return adminError;

            var bulletin = await this.Db.System_Bulletins.FindAsync(id);

            if (bulletin == null)
                return StatusCode(StatusCodes.Status404NotFound);

            // don't delete the record, just set it to be inactive
            bulletin.IsActive = false;
            await this.Db.SaveChangesAsync();

            return StatusCode(StatusCodes.Status204NoContent);
        }




        //  not an admin endpoint - but needs the `GetCurrentUserAsync()` method.
        [HttpPost("{id:guid}/dismiss")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DismissBulletin(Guid id)
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;


            var bulletin = await this.Db.System_Bulletins
                .FirstOrDefaultAsync(b => b.Id == id && b.IsActive && b.IsDismissible);

            if (bulletin == null)
                return StatusCode(StatusCodes.Status404NotFound);

            var alreadyDismissed = await this.Db.Log_SystemBulletinEntryDismissals
                .AnyAsync(d => d.SystemBulletinId == id && d.CreatedBy == user.Id);

            if (!alreadyDismissed)
            {
                this.Db.Log_SystemBulletinEntryDismissals.Add(new LogSystemBulletinEntryDismissalEventEntityModel
                {
                    Id = UUIDUtilities.GenerateV5(DatabaseObjectTypeEnum.Log_SystemBulletin_EntryDismissal_EventEntry),
                    CreatedBy = user.Id,
                    CreatedAtUtc = DateTime.UtcNow,
                    SystemBulletinId = id,
                });
                await this.Db.SaveChangesAsync();
            }

            return StatusCode(StatusCodes.Status204NoContent);
        }
    }
}
