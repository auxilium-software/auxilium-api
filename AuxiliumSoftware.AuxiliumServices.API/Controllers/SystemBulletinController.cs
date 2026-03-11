using AuxiliumSoftware.AuxiliumServices.API.Models;
using AuxiliumSoftware.AuxiliumServices.API.Models.SystemBulletin;
using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework;
using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework.EntityModels;
using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework.Enumerators;
using AuxiliumSoftware.AuxiliumServices.Common.Enumerators;
using AuxiliumSoftware.AuxiliumServices.Common.Services;
using AuxiliumSoftware.AuxiliumServices.Common.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AuxiliumSoftware.AuxiliumServices.API.Controllers
{
    [ApiController]
    [Route("/api/v3/system-bulletin")]
    [Tags("System Bulletin")]
    [Authorize]
    public class SystemBulletinController : ControllerBase
    {
        private readonly IWebApplicationFirewallService _waf;
        private readonly ILogger<SystemBulletinController> _logger;
        private readonly AuxiliumDbContext _db;

        public SystemBulletinController(
            IConfiguration configuration,
            AuxiliumDbContext db,
            IWebApplicationFirewallService waf,
            ILogger<SystemBulletinController> logger,
            ITotpService totpService
            )
        {
            _waf = waf;
            _logger = logger;
            _db = db;
        }

        [AllowAnonymous]
        [HttpGet("")]
        [ProducesResponseType(typeof(List<SystemBulletinResponseModel>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<SystemBulletinResponseModel>>> GetMyMessages()
        {
            var now = DateTime.UtcNow;
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAuthenticated = userId != null;
            Guid? userGuid = isAuthenticated ? Guid.Parse(userId) : null;

            var query = _db.System_Bulletins
                .Where(b => b.IsActive)
                .Where(b => b.StartsAt <= now)
                .Where(b => b.EndsAt == null || b.EndsAt > now);

            // filter by audience
            query = query.Where(b =>
                b.TargetAudience == SystemBulletinMessageTargetAudienceEnum.Everyone ||
                (b.TargetAudience == SystemBulletinMessageTargetAudienceEnum.LoggedInUsersOnly && isAuthenticated) ||
                (b.TargetAudience == SystemBulletinMessageTargetAudienceEnum.PublicOnly && !isAuthenticated) ||
                (b.TargetAudience == SystemBulletinMessageTargetAudienceEnum.SingleUserOnly && b.SpecificUserId == userGuid));

            // exclude dismissed bulletins for authenticated users
            if (isAuthenticated)
            {
                query = query.Where(b =>
                    !b.IsDismissible ||
                    !b.Dismissals.Any(d => d.CreatedBy == userGuid));
            }

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
    }
}
