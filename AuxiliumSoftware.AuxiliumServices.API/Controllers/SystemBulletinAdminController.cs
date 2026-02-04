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
            IConfiguration configuration,
            AuxiliumDbContext db,
            ILogger<SystemBulletinAdminController> logger,
            ITotpService totpService
        )
        : base(configuration, db, logger, totpService)
        {
        }



        [HttpGet("/all")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<List<SystemBulletinResponseModel>>> GetAllMessages()
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            var adminError = await RequireAdminAsync();
            if (adminError != null) return adminError;

            var query = this.Db.SystemBulletins;

            var bulletins = await query
                .OrderByDescending(b => b.Severity)
                .ThenByDescending(b => b.CreatedAt)
                .Select(b => new SystemBulletinResponseModel
                {
                    Id = b.Id,
                    Severity = b.Severity,
                    Title = b.Title,
                    Content = b.Content,
                    IsDismissible = b.IsDismissible,
                    CreatedAt = b.CreatedAt
                })
                .ToListAsync();

            return Ok(bulletins);
        }

        [HttpPost("")]
        [ProducesResponseType(typeof(SystemBulletinResponseModel), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<SystemBulletinResponseModel>> CreateBulletin(
            [FromBody] SystemBulletinCreationRequestModel request)
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            var adminError = await RequireAdminAsync();
            if (adminError != null) return adminError;


            var bulletin = new SystemBulletinEntryEntityModel
            {
                Id = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,
                CreatedBy = user.Id,
                Severity = request.Severity,
                Title = request.Title,
                Content = request.Content,
                IsDismissible = request.IsDismissible,
                IsActive = true,
                StartsAt = request.StartsAt ?? DateTime.UtcNow,
                EndsAt = request.EndsAt,
                TargetAudience = request.TargetAudience,
                SpecificUserId = request.SpecificUserId
            };

            this.Db.SystemBulletins.Add(bulletin);
            await this.Db.SaveChangesAsync();

            return CreatedAtAction(nameof(CreateBulletin), new SystemBulletinResponseModel
            {
                Id = bulletin.Id,
                Severity = bulletin.Severity,
                Title = bulletin.Title,
                Content = bulletin.Content,
                IsDismissible = bulletin.IsDismissible,
                CreatedAt = bulletin.CreatedAt
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

            var bulletin = await this.Db.SystemBulletins.FindAsync(id);

            if (bulletin == null)
                return NotFound();

            // don't delete the record, just set it to be inactive
            bulletin.IsActive = false;
            await this.Db.SaveChangesAsync();

            return NoContent();
        }





        [HttpPost("{id:guid}/dismiss")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DismissBulletin(Guid id)
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;


            var bulletin = await this.Db.SystemBulletins
                .FirstOrDefaultAsync(b => b.Id == id && b.IsActive && b.IsDismissible);

            if (bulletin == null)
                return NotFound();

            var alreadyDismissed = await this.Db.Log_SystemBulletinEntryDismissals
                .AnyAsync(d => d.SystemBulletinId == id && d.CreatedBy == user.Id);

            if (!alreadyDismissed)
            {
                this.Db.Log_SystemBulletinEntryDismissals.Add(new LogSystemBulletinEntryDismissalEventEntityModel
                {
                    Id = UUIDUtilities.GenerateV5(DatabaseObjectType.LogSystemBulletinEntryDismissalEvent),
                    CreatedBy = user.Id,
                    CreatedAt = DateTime.UtcNow,
                    SystemBulletinId = id,
                });
                await this.Db.SaveChangesAsync();
            }

            return NoContent();
        }
    }
}
