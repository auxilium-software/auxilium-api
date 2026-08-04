using AuxiliumSoftware.AuxiliumServices.API.Common.ControllerBases;
using AuxiliumSoftware.AuxiliumServices.API.Models.Waf.RequestModels;
using AuxiliumSoftware.AuxiliumServices.API.Models.Waf.ResponseModels;
using AuxiliumSoftware.AuxiliumServices.API.Models.Waf.ResponseModels.SubResponseModels;
using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework;
using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework.EntityModels;
using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework.Enumerators;
using AuxiliumSoftware.AuxiliumServices.Common.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace AuxiliumSoftware.AuxiliumServices.API.Controllers
{
    [ApiController]
    [Route("/api/v3/web-application-firewall")]
    [Tags("Web Application Firewall")]
    public class WebApplicationFirewallController : LoggedInControllerBase
    {
        public WebApplicationFirewallController(
            ISystemSettingsService systemSettingsService,
            IConfiguration configuration,
            AuxiliumDbContext db,
            IWebApplicationFirewallService waf,
            ILogger<WebApplicationFirewallController> logger,
            ITotpService totpService
        )
        : base(systemSettingsService, configuration, db, waf, logger, totpService)
        {
        }


        #region Statistics
        [HttpGet("statistics")]
        [ProducesResponseType(typeof(WafStatisticsResponseModel), StatusCodes.Status200OK)]
        public async Task<ActionResult<WafStatisticsResponseModel>> GetStatistics(
            [FromQuery] int? hoursBack = 24
        )
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;
            var adminError = await this.RequireAdminAsync();
            if (adminError != null) return adminError;

            var now = DateTime.UtcNow;
            var since = now.AddHours(-Math.Abs(hoursBack ?? 24));

            var totalLoginAttempts = await this.Db.Log_LoginAttempts
                .Where(l => l.CreatedAtUtc >= since)
                .CountAsync();

            var failedLoginAttempts = await this.Db.Log_LoginAttempts
                .Where(l => l.CreatedAtUtc >= since && !l.WasLoginSuccessful)
                .CountAsync();

            var successfulLoginAttempts = await this.Db.Log_LoginAttempts
                .Where(l => l.CreatedAtUtc >= since && l.WasLoginSuccessful)
                .CountAsync();

            var blockedIpAttempts = await this.Db.Log_LoginAttempts
                .Where(l => l.CreatedAtUtc >= since && l.WasBlockedByWaf)
                .CountAsync();

            var currentlyBlockedIps = await this.Db.System_Waf_IpBlacklist
                .Where(b => b.UnblacklistedAtUtc == null && (b.ExpiresAtUtc == null || b.ExpiresAtUtc > now))
                .CountAsync();

            var permanentlyBannedIps = await this.Db.System_Waf_IpBlacklist
                .Where(b => b.UnblacklistedAtUtc == null && b.IsPermanent)
                .CountAsync();

            var temporarilyBlockedIps = await this.Db.System_Waf_IpBlacklist
                .Where(b => b.UnblacklistedAtUtc == null && !b.IsPermanent && b.ExpiresAtUtc > now)
                .CountAsync();

            var lockedOutUsers = await this.Db.System_Waf_UserBlacklist
                .Where(b => b.UnblacklistedAtUtc == null && (b.ExpiresAtUtc == null || b.ExpiresAtUtc > now))
                .Select(b => b.UserId)
                .Distinct()
                .CountAsync();

            var distinctIpsWithFailures = await this.Db.Log_LoginAttempts
                .Where(l => l.CreatedAtUtc >= since && !l.WasLoginSuccessful)
                .Select(l => l.ClientIpAddress)
                .Distinct()
                .CountAsync();

            var distinctUsersTargeted = await this.Db.Log_LoginAttempts
                .Where(l => l.CreatedAtUtc >= since && !l.WasLoginSuccessful && l.TargetUserId != null)
                .Select(l => l.TargetUserId)
                .Distinct()
                .CountAsync();

            var loginAttemptsRaw = await this.Db.Log_LoginAttempts
                .Where(l => l.CreatedAtUtc >= since)
                .Select(l => new { l.CreatedAtUtc, l.WasLoginSuccessful, l.WasBlockedByWaf })
                .ToListAsync();

            var hourlyBreakdown = loginAttemptsRaw
                .GroupBy(l => new DateTime(l.CreatedAtUtc.Year, l.CreatedAtUtc.Month, l.CreatedAtUtc.Day, l.CreatedAtUtc.Hour, 0, 0, DateTimeKind.Utc))
                .Select(g => new HourlyBreakdownItem
                {
                    Hour = g.Key,
                    TotalAttempts = g.Count(),
                    FailedAttempts = g.Count(x => !x.WasLoginSuccessful),
                    BlockedAttempts = g.Count(x => x.WasBlockedByWaf)
                })
                .OrderBy(x => x.Hour)
                .ToList();

            var failedAttemptsRaw = await this.Db.Log_LoginAttempts
                .Where(l => l.CreatedAtUtc >= since && !l.WasLoginSuccessful)
                .Select(l => new { l.ClientIpAddress, l.TargetUserId, l.CreatedAtUtc })
                .ToListAsync();

            var topOffendingIps = failedAttemptsRaw
                .GroupBy(l => l.ClientIpAddress)
                .Select(g => new TopOffenderItem
                {
                    IpAddress = IPAddress.Parse(g.Key.ToString()),
                    FailedAttempts = g.Count(),
                    DistinctUsersTargeted = g.Select(x => x.TargetUserId).Distinct().Count(),
                    LastAttempt = g.Max(x => x.CreatedAtUtc)
                })
                .OrderByDescending(x => x.FailedAttempts)
                .Take(10)
                .ToList();

            var offendingIpAddresses = topOffendingIps.Select(x => x.IpAddress).ToList();
            var blockedIpSet = await this.Db.System_Waf_IpBlacklist
                .Where(b => b.UnblacklistedAtUtc == null && (b.ExpiresAtUtc == null || b.ExpiresAtUtc > now) && offendingIpAddresses.Contains(b.IpAddress))
                .Select(b => b.IpAddress)
                .ToListAsync();

            foreach (var offender in topOffendingIps)
            {
                offender.IsCurrentlyBlocked = blockedIpSet.Contains(offender.IpAddress);
            }

            var targetedAttemptsRaw = await this.Db.Log_LoginAttempts
                .Where(l => l.CreatedAtUtc >= since && !l.WasLoginSuccessful && l.TargetUserId != null)
                .Select(l => new { l.TargetUserId, l.ClientIpAddress, l.CreatedAtUtc })
                .ToListAsync();

            var topTargetedUsers = targetedAttemptsRaw
                .GroupBy(l => l.TargetUserId)
                .Select(g => new TopTargetedUserItem
                {
                    UserId = g.Key!.Value,
                    FailedAttempts = g.Count(),
                    DistinctIpAddresses = g.Select(x => x.ClientIpAddress).Distinct().Count(),
                    LastAttempt = g.Max(x => x.CreatedAtUtc)
                })
                .OrderByDescending(x => x.FailedAttempts)
                .Take(10)
                .ToList();

            var targetedUserIds = topTargetedUsers.Select(x => x.UserId).ToList();
            var userDetails = await this.Db.Users
                .Where(u => targetedUserIds.Contains(u.Id))
                .Select(u => new { u.Id, u.EmailAddress, u.FullName })
                .ToDictionaryAsync(u => u.Id);

            var lockedUserIds = await this.Db.System_Waf_UserBlacklist
                .Where(b => targetedUserIds.Contains(b.UserId) && b.UnblacklistedAtUtc == null && (b.ExpiresAtUtc == null || b.ExpiresAtUtc > now))
                .Select(b => b.UserId)
                .Distinct()
                .ToListAsync();

            foreach (TopTargetedUserItem target in topTargetedUsers)
            {
                target.IsCurrentlyLockedOut = lockedUserIds.Contains(target.UserId);
            }

            var wafEnabled = await this.SystemSettings.GetBoolAsync(SystemSettingKeyEnum.Policies_WebApplicationFirewall_Enabled);

            return StatusCode(StatusCodes.Status200OK, new WafStatisticsResponseModel
            {
                WafEnabled = wafEnabled,
                PeriodStart = since,
                PeriodEnd = now,
                TotalLoginAttempts = totalLoginAttempts,
                SuccessfulLoginAttempts = successfulLoginAttempts,
                FailedLoginAttempts = failedLoginAttempts,
                BlockedByWafAttempts = blockedIpAttempts,
                CurrentlyBlockedIps = currentlyBlockedIps,
                PermanentlyBannedIps = permanentlyBannedIps,
                TemporarilyBlockedIps = temporarilyBlockedIps,
                LockedOutUsers = lockedOutUsers,
                DistinctOffendingIps = distinctIpsWithFailures,
                DistinctTargetedUsers = distinctUsersTargeted,
                HourlyBreakdown = hourlyBreakdown,
                TopOffendingIps = topOffendingIps,
                TopTargetedUsers = topTargetedUsers
            });
        }
        #endregion


        #region Login Attempts
        [HttpGet("login-attempts")]
        [ProducesResponseType(typeof(LoginAttemptsResponseModel), StatusCodes.Status200OK)]
        public async Task<ActionResult<LoginAttemptsResponseModel>> GetLoginAttempts(
            [FromQuery] string? ipAddress = null,
            [FromQuery] Guid? userId = null,
            [FromQuery] string? email = null,
            [FromQuery] bool? successfulOnly = null,
            [FromQuery] bool? failedOnly = null,
            [FromQuery] bool? blockedOnly = null,
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50
        )
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;
            var adminError = await this.RequireAdminAsync();
            if (adminError != null) return adminError;

            var query = this.Db.Log_LoginAttempts.AsQueryable();

            if (!string.IsNullOrWhiteSpace(ipAddress))
                query = query.Where(l => l.ClientIpAddress == ipAddress);

            if (userId.HasValue)
                query = query.Where(l => l.TargetUserId == userId.Value);

            if (!string.IsNullOrWhiteSpace(email))
                query = query.Where(l => l.AttemptedEmailAddress != null && l.AttemptedEmailAddress.Contains(email));

            if (successfulOnly == true)
                query = query.Where(l => l.WasLoginSuccessful);

            if (failedOnly == true)
                query = query.Where(l => !l.WasLoginSuccessful);

            if (blockedOnly == true)
                query = query.Where(l => l.WasBlockedByWaf);

            if (from.HasValue)
                query = query.Where(l => l.CreatedAtUtc >= from.Value);

            if (to.HasValue)
                query = query.Where(l => l.CreatedAtUtc <= to.Value);

            var totalCount = await query.CountAsync();

            var attempts = await query
                .OrderByDescending(l => l.CreatedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(l => new LoginAttemptItem
                {
                    Id = l.Id,
                    AttemptedAt = l.CreatedAtUtc,
                    IpAddress = IPAddress.Parse(l.ClientIpAddress.ToString()),
                    TargetEmail = l.AttemptedEmailAddress,
                    TargetUserId = l.TargetUserId,
                    WasSuccessful = l.WasLoginSuccessful,
                    WasBlockedByWaf = l.WasBlockedByWaf,
                    FailureReason = l.FailureReason,
                })
                .ToListAsync();

            return StatusCode(StatusCodes.Status200OK, new LoginAttemptsResponseModel
            {
                Attempts = attempts,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            });
        }
        #endregion


        #region IP Blacklist
        [HttpGet("blacklist/ips")]
        [ProducesResponseType(typeof(BlacklistedIpAddressesResponseModel), StatusCodes.Status200OK)]
        public async Task<ActionResult<BlacklistedIpAddressesResponseModel>> GetBlacklistedIpAddresses(
            [FromQuery] bool includeExpired = false,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50
        )
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;
            var adminError = await this.RequireAdminAsync();
            if (adminError != null) return adminError;

            var now = DateTime.UtcNow;
            var query = this.Db.System_Waf_IpBlacklist.AsQueryable();

            if (!includeExpired)
            {
                query = query.Where(b => b.UnblacklistedAtUtc == null && (b.ExpiresAtUtc == null || b.ExpiresAtUtc > now));
            }

            var totalCount = await query.CountAsync();

            var blocks = await query
                .OrderByDescending(b => b.CreatedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(b => new BlacklistedIpAddressItem
                {
                    Id = b.Id,
                    IpAddress = b.IpAddress,
                    Justification = b.JustificationForBlacklist,
                    IsPermanent = b.IsPermanent,
                    IsActive = b.UnblacklistedAtUtc == null && (b.ExpiresAtUtc == null || b.ExpiresAtUtc > now),
                    BlockedAt = b.CreatedAtUtc,
                    ExpiresAt = b.ExpiresAtUtc,
                    UnblockedAt = b.UnblacklistedAtUtc,
                    UnblockedByUserId = b.UnblacklistedBy,
                    UnblockJustification = b.JustificationForUnblacklist
                })
                .ToListAsync();

            return StatusCode(StatusCodes.Status200OK, new BlacklistedIpAddressesResponseModel
            {
                Blocks = blocks,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            });
        }


        [HttpPost("blacklist/ips")]
        [ProducesResponseType(typeof(BlacklistedIpAddressItem), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<BlacklistedIpAddressItem>> BlacklistIpAddress([FromBody] BlacklistIpAddressRequestModel request)
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;
            var adminError = await this.RequireAdminAsync();
            if (adminError != null) return adminError;

            if (string.IsNullOrWhiteSpace(request.Reason))
            {
                return StatusCode(StatusCodes.Status400BadRequest, new { message = "Reason is required" });
            }

            var now = DateTime.UtcNow;
            var normalizedIp = Waf.NormaliseIpAddress(request.IpAddress);

            // Check if already blocked
            var existingBlock = await this.Db.System_Waf_IpBlacklist
                .Where(b => b.IpAddress == request.IpAddress && b.UnblacklistedAtUtc == null && (b.ExpiresAtUtc == null || b.ExpiresAtUtc > now))
                .FirstOrDefaultAsync();

            if (existingBlock != null)
            {
                return StatusCode(StatusCodes.Status400BadRequest, new { message = "This IP is already blocked" });
            }

            // Check against whitelist
            if (await Waf.IsWhitelistedAsync(request.IpAddress))
            {
                return StatusCode(StatusCodes.Status400BadRequest, new { message = "Cannot block a whitelisted IP address. Remove it from the whitelist first." });
            }

            await Waf.BlacklistIpAddressAsync(
                ipAddress: request.IpAddress,
                reason: $"[Manual] {request.Reason}",
                adminUser: user!,
                permanent: request.IsPermanent
            );

            // Fetch the created block to return
            var block = await this.Db.System_Waf_IpBlacklist
                .Where(b => b.IpAddress == request.IpAddress && b.UnblacklistedAtUtc == null)
                .OrderByDescending(b => b.CreatedAtUtc)
                .FirstAsync();

            return StatusCode(StatusCodes.Status201Created, new BlacklistedIpAddressItem
            {
                Id = block.Id,
                IpAddress = block.IpAddress,
                Justification = block.JustificationForBlacklist,
                IsPermanent = block.IsPermanent,
                IsActive = true,
                BlockedAt = block.CreatedAtUtc,
                ExpiresAt = block.ExpiresAtUtc
            });
        }


        [HttpGet("blacklist/ips/{ipAddress}")]
        [ProducesResponseType(typeof(BlacklistedIpAddressDetailResponseModel), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<BlacklistedIpAddressDetailResponseModel>> GetIpAddressBlacklistEntryDetails(IPAddress ipAddress)
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;
            var adminError = await this.RequireAdminAsync();
            if (adminError != null) return adminError;

            var now = DateTime.UtcNow;
            var normalizedIp = Waf.NormaliseIpAddress(ipAddress);

            var block = await this.Db.System_Waf_IpBlacklist
                .Where(b => b.IpAddress == ipAddress)
                .OrderByDescending(b => b.CreatedAtUtc)
                .FirstOrDefaultAsync();

            if (block == null)
            {
                return StatusCode(StatusCodes.Status404NotFound, new { message = $"No block record found for IP: {ipAddress}" });
            }

            var recentAttempts = await this.Db.Log_LoginAttempts
                .Where(l => l.ClientIpAddress == normalizedIp)
                .OrderByDescending(l => l.CreatedAtUtc)
                .Take(50)
                .Select(l => new LoginAttemptItem
                {
                    Id = l.Id,
                    AttemptedAt = l.CreatedAtUtc,
                    IpAddress = IPAddress.Parse(l.ClientIpAddress.ToString()),
                    WasSuccessful = l.WasLoginSuccessful,
                    WasBlockedByWaf = l.WasBlockedByWaf,
                    FailureReason = l.FailureReason,
                    TargetEmail = l.AttemptedEmailAddress,
                    TargetUserId = l.TargetUserId
                })
                .ToListAsync();

            var blockHistory = await this.Db.System_Waf_IpBlacklist
                .Where(b => b.IpAddress == ipAddress)
                .OrderByDescending(b => b.CreatedAtUtc)
                .Select(b => new BlacklistHistoryItem
                {
                    Id = b.Id,
                    BlockedAt = b.CreatedAtUtc,
                    ExpiresAt = b.ExpiresAtUtc,
                    IsPermanent = b.IsPermanent,
                    Reason = b.JustificationForBlacklist,
                    WasManuallyUnblocked = b.UnblacklistedAtUtc != null,
                    UnblockedAt = b.UnblacklistedAtUtc,
                    UnblockReason = b.JustificationForUnblacklist
                })
                .ToListAsync();

            var isCurrentlyActive = block.UnblacklistedAtUtc == null && (block.ExpiresAtUtc == null || block.ExpiresAtUtc > now);

            return StatusCode(StatusCodes.Status200OK, new BlacklistedIpAddressDetailResponseModel
            {
                IpAddress = ipAddress,
                CurrentBlock = isCurrentlyActive
                    ? new BlacklistedIpAddressItem
                    {
                        Id = block.Id,
                        IpAddress = block.IpAddress,
                        Justification = block.JustificationForBlacklist,
                        IsPermanent = block.IsPermanent,
                        IsActive = true,
                        BlockedAt = block.CreatedAtUtc,
                        ExpiresAt = block.ExpiresAtUtc
                    }
                    : null,
                RecentLoginAttempts = recentAttempts,
                BlockHistory = blockHistory,
                TotalFailedAttempts = recentAttempts.Count(a => !a.WasSuccessful),
                TotalBlockedAttempts = recentAttempts.Count(a => a.WasBlockedByWaf),
                FirstSeenAt = recentAttempts.Any() ? recentAttempts.Min(a => a.AttemptedAt) : null,
                LastSeenAt = recentAttempts.Any() ? recentAttempts.Max(a => a.AttemptedAt) : null
            });
        }


        [HttpDelete("blacklist/ips/{ipAddress}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RemoveIpAddressFromBlacklist(string ipAddress, [FromQuery] string? reason = null)
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;
            var adminError = await this.RequireAdminAsync();
            if (adminError != null) return adminError;

            ipAddress = Uri.UnescapeDataString(ipAddress);

            if (!IPAddress.TryParse(ipAddress, out var parsedIpAddress))
            {
                return StatusCode(StatusCodes.Status400BadRequest, new { message = "Invalid IP address format" });
            }

            var unblocked = await Waf.RemoveIpAddressFromBlacklistAsync(parsedIpAddress, user!);

            if (!unblocked)
            {
                return StatusCode(StatusCodes.Status404NotFound, new { message = $"No active block found for IP: {ipAddress}" });
            }

            return StatusCode(StatusCodes.Status204NoContent);
        }
        #endregion


        #region User Blacklist
        [HttpGet("blacklist/users")]
        [ProducesResponseType(typeof(LockedOutUsersResponseModel), StatusCodes.Status200OK)]
        public async Task<ActionResult<LockedOutUsersResponseModel>> GetBlacklistedUsers(
            [FromQuery] bool includeExpired = false,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50
        )
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;
            var adminError = await this.RequireAdminAsync();
            if (adminError != null) return adminError;

            var now = DateTime.UtcNow;

            var query = this.Db.System_Waf_UserBlacklist.AsQueryable();

            if (!includeExpired)
            {
                query = query.Where(b => b.UnblacklistedAtUtc == null && (b.ExpiresAtUtc == null || b.ExpiresAtUtc > now));
            }

            var totalCount = await query.Select(b => b.UserId).Distinct().CountAsync();

            // grab the ids of the most recent entry per user
            var latestEntryIds = await query
                .GroupBy(b => b.UserId)
                .Select(g => g.OrderByDescending(b => b.CreatedAtUtc).First().Id)
                .ToListAsync();

            // then fetch the full entities with pagination
            var userBlocks = await query
                .Where(b => latestEntryIds.Contains(b.Id))
                .OrderByDescending(b => b.CreatedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var userIds = userBlocks.Select(b => b.UserId).ToList();

            var userDetails = await this.Db.Users
                .Where(u => userIds.Contains(u.Id))
                .Select(u => new { u.Id, u.EmailAddress, u.FullName })
                .ToDictionaryAsync(u => u.Id);

            var recentAttemptCounts = await this.Db.Log_LoginAttempts
                .Where(l => l.TargetUserId != null && userIds.Contains(l.TargetUserId.Value) && !l.WasLoginSuccessful)
                .Where(l => l.CreatedAtUtc >= now.AddHours(-24))
                .GroupBy(l => l.TargetUserId)
                .Select(g => new { UserId = g.Key, Count = g.Count(), DistinctIps = g.Select(x => x.ClientIpAddress).Distinct().Count() })
                .ToDictionaryAsync(x => x.UserId!.Value);

            var lockedUsers = userBlocks.Select(b =>
            {
                var item = new BlacklistedUserItem
                {
                    UserId = b.UserId,
                    IsLockedOut = b.UnblacklistedAtUtc == null && (b.ExpiresAtUtc == null || b.ExpiresAtUtc > now),
                    LockedOutAt = b.CreatedAtUtc,
                    LockoutEndsAt = b.ExpiresAtUtc,
                    LockoutReason = b.JustificationForBlacklist
                };

                if (recentAttemptCounts.TryGetValue(b.UserId, out var stats))
                {
                    item.RecentFailedAttempts24h = stats.Count;
                    item.DistinctIpAddresses24h = stats.DistinctIps;
                }

                return item;
            }).ToList();

            return StatusCode(StatusCodes.Status200OK, new LockedOutUsersResponseModel
            {
                Users = lockedUsers,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            });
        }


        [HttpPost("blacklist/users/{userId:guid}")]
        [ProducesResponseType(typeof(BlacklistedUserItem), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<BlacklistedUserItem>> BlacklistUser(Guid userId, [FromBody] BlacklistUserRequestModel request)
        {
            var (adminUser, error) = await GetCurrentUserAsync();
            if (error != null) return error;
            var adminError = await this.RequireAdminAsync();
            if (adminError != null) return adminError;

            if (string.IsNullOrWhiteSpace(request.Reason))
            {
                return StatusCode(StatusCodes.Status400BadRequest, new { message = "Reason is required" });
            }

            var targetUser = await this.Db.Users.FindAsync(userId);
            if (targetUser == null)
            {
                return StatusCode(StatusCodes.Status404NotFound, new { message = "User not found" });
            }

            var now = DateTime.UtcNow;

            // Check if already locked
            var existingLock = await this.Db.System_Waf_UserBlacklist
                .Where(b => b.UserId == userId && b.UnblacklistedAtUtc == null && (b.ExpiresAtUtc == null || b.ExpiresAtUtc > now))
                .FirstOrDefaultAsync();

            if (existingLock != null)
            {
                return StatusCode(StatusCodes.Status400BadRequest, new { message = "User is already locked out" });
            }

            await Waf.BlacklistUserAsync(
                user: targetUser,
                reason: $"[Manual] {request.Reason}",
                adminUser: adminUser!,
                permanent: request.IsPermanent
            );

            var block = await this.Db.System_Waf_UserBlacklist
                .Where(b => b.UserId == userId && b.UnblacklistedAtUtc == null)
                .OrderByDescending(b => b.CreatedAtUtc)
                .FirstAsync();

            return StatusCode(StatusCodes.Status201Created, new BlacklistedUserItem
            {
                UserId = targetUser.Id,
                IsLockedOut = true,
                LockedOutAt = block.CreatedAtUtc,
                LockoutEndsAt = block.ExpiresAtUtc,
                LockoutReason = block.JustificationForBlacklist,
            });
        }


        [HttpPost("blacklist/users/{userId:guid}/unlock")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RemoveUserFromBlacklist(Guid userId, [FromBody] UnlockUserRequestModel? request = null)
        {
            var (adminUser, error) = await GetCurrentUserAsync();
            if (error != null) return error;
            var adminError = await this.RequireAdminAsync();
            if (adminError != null) return adminError;

            var targetUser = await this.Db.Users.FindAsync(userId);
            if (targetUser == null)
            {
                return StatusCode(StatusCodes.Status404NotFound, new { message = "User not found" });
            }

            var unlocked = await Waf.RemoveUserFromBlacklist(targetUser, adminUser!);

            if (!unlocked)
            {
                return StatusCode(StatusCodes.Status400BadRequest, new { message = "User is not currently locked out" });
            }

            return StatusCode(StatusCodes.Status204NoContent);
        }
        #endregion


        #region IP Whitelist
        [HttpGet("whitelist/ips")]
        [ProducesResponseType(typeof(WhitelistedIpsResponseModel), StatusCodes.Status200OK)]
        public async Task<ActionResult<WhitelistedIpsResponseModel>> GetWhitelistedIpAddresses(
            [FromQuery] bool includeExpired = false,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50
        )
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;
            var adminError = await this.RequireAdminAsync();
            if (adminError != null) return adminError;

            var now = DateTime.UtcNow;
            var query = this.Db.System_Waf_IpWhitelist.AsQueryable();

            if (!includeExpired)
            {
                query = query.Where(w => w.UnwhitelistedAtUtc == null && (w.ExpiresAtUtc == null || w.ExpiresAtUtc > now));
            }

            var totalCount = await query.CountAsync();

            var entries = await query
                .OrderByDescending(w => w.CreatedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(w => new WhitelistedIpItem
                {
                    Id = w.Id,
                    IpAddress = w.IpAddress,
                    Reason = w.JustificationForWhitelist,
                    IsPermanent = w.IsPermanent,
                    IsActive = w.UnwhitelistedAtUtc == null && (w.ExpiresAtUtc == null || w.ExpiresAtUtc > now),
                    WhitelistedAt = w.CreatedAtUtc,
                    WhitelistedByUserId = w.CreatedByUserId,
                    ExpiresAt = w.ExpiresAtUtc,
                    RemovedAt = w.UnwhitelistedAtUtc,
                    RemovedByUserId = w.UnwhitelistedBy,
                    RemovalReason = w.JustificationForUnwhitelist
                })
                .ToListAsync();

            return StatusCode(StatusCodes.Status200OK, new WhitelistedIpsResponseModel
            {
                Entries = entries,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            });
        }


        [HttpPost("whitelist/ips")]
        [ProducesResponseType(typeof(WhitelistedIpItem), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<WhitelistedIpItem>> WhitelistIpAddress([FromBody] WhitelistIpRequestModel request)
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;
            var adminError = await this.RequireAdminAsync();
            if (adminError != null) return adminError;

            if (string.IsNullOrWhiteSpace(request.Reason))
            {
                return StatusCode(StatusCodes.Status400BadRequest, new { message = "Reason is required." });
            }

            if (!IPAddress.TryParse(request.IpAddress, out var parsedIpAddress))
            {
                return StatusCode(StatusCodes.Status400BadRequest, new { message = "Invalid IP Address format." });
            }

            var now = DateTime.UtcNow;
            var normalizedIp = Waf.NormaliseIpAddress(parsedIpAddress);

            // Check if already whitelisted
            var existingEntry = await this.Db.System_Waf_IpWhitelist
                .Where(w => w.IpAddress == parsedIpAddress && w.UnwhitelistedAtUtc == null && (w.ExpiresAtUtc == null || w.ExpiresAtUtc > now))
                .FirstOrDefaultAsync();

            if (existingEntry != null)
            {
                return StatusCode(StatusCodes.Status400BadRequest, new { message = "This IP is already whitelisted" });
            }

            await Waf.WhitelistIpAddressAsync(
                ipAddress: parsedIpAddress,
                reason: request.Reason,
                adminUser: user!,
                permanent: request.IsPermanent
            );

            var entry = await this.Db.System_Waf_IpWhitelist
                .Where(w => w.IpAddress == parsedIpAddress && w.UnwhitelistedAtUtc == null)
                .OrderByDescending(w => w.CreatedAtUtc)
                .FirstAsync();

            return StatusCode(StatusCodes.Status201Created, new WhitelistedIpItem
            {
                Id = entry.Id,
                IpAddress = entry.IpAddress,
                Reason = entry.JustificationForWhitelist,
                IsPermanent = entry.IsPermanent,
                IsActive = true,
                WhitelistedAt = entry.CreatedAtUtc,
                WhitelistedByUserId = entry.CreatedByUserId,
                ExpiresAt = entry.ExpiresAtUtc
            });
        }


        [HttpDelete("whitelist/ips/{ipAddress}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RemoveIpAddressFromWhitelist(string ipAddress, [FromQuery] string? reason = null)
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;
            var adminError = await this.RequireAdminAsync();
            if (adminError != null) return adminError;

            ipAddress = Uri.UnescapeDataString(ipAddress);

            if (!IPAddress.TryParse(ipAddress, out var parsedIpAddress))
            {
                return StatusCode(StatusCodes.Status400BadRequest, new { message = "Invalid IP address format" });
            }

            var removed = await Waf.RemoveIpAddressFromWhitelistAsync(parsedIpAddress, user!);

            if (!removed)
            {
                return StatusCode(StatusCodes.Status404NotFound, new { message = $"No active whitelist entry found for IP: {ipAddress}" });
            }

            return StatusCode(StatusCodes.Status204NoContent);
        }
        #endregion


        #region User Whitelist
        [HttpGet("whitelist/users")]
        [ProducesResponseType(typeof(WhitelistedUsersResponseModel), StatusCodes.Status200OK)]
        public async Task<ActionResult<WhitelistedUsersResponseModel>> GetWhitelistedUsers(
            [FromQuery] bool includeExpired = false,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50
        )
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;
            var adminError = await this.RequireAdminAsync();
            if (adminError != null) return adminError;

            var now = DateTime.UtcNow;
            var query = this.Db.System_Waf_UserWhitelist.AsQueryable();

            if (!includeExpired)
            {
                query = query.Where(w => w.UnwhitelistedAtUtc == null && (w.ExpiresAtUtc == null || w.ExpiresAtUtc > now));
            }

            var totalCount = await query.Select(w => w.UserId).Distinct().CountAsync();

            var userWhitelists = await query
                .GroupBy(w => w.UserId)
                .Select(g => g.OrderByDescending(w => w.CreatedAtUtc).First())
                .OrderByDescending(w => w.CreatedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var userIds = userWhitelists.Select(w => w.UserId).ToList();

            var userDetails = await this.Db.Users
                .Where(u => userIds.Contains(u.Id))
                .Select(u => new { u.Id, u.EmailAddress, u.FullName })
                .ToDictionaryAsync(u => u.Id);

            var entries = userWhitelists.Select(w =>
            {
                var item = new WhitelistedUserItem
                {
                    UserId = w.UserId,
                    Reason = w.JustificationForWhitelist,
                    IsPermanent = w.IsPermanent,
                    IsActive = w.UnwhitelistedAtUtc == null && (w.ExpiresAtUtc == null || w.ExpiresAtUtc > now),
                    WhitelistedAt = w.CreatedAtUtc,
                    WhitelistedByUserId = w.CreatedByUserId,
                    ExpiresAt = w.ExpiresAtUtc,
                    RemovedAt = w.UnwhitelistedAtUtc,
                    RemovedByUserId = w.UnwhitelistedBy,
                    RemovalReason = w.JustificationForUnwhitelist
                };

                return item;
            }).ToList();

            return StatusCode(StatusCodes.Status200OK, new WhitelistedUsersResponseModel
            {
                Entries = entries,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            });
        }


        [HttpPost("whitelist/users/{userId:guid}")]
        [ProducesResponseType(typeof(WhitelistedUserItem), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<WhitelistedUserItem>> WhitelistUser(Guid userId, [FromBody] WhitelistUserRequestModel request)
        {
            var (adminUser, error) = await GetCurrentUserAsync();
            if (error != null) return error;
            var adminError = await this.RequireAdminAsync();
            if (adminError != null) return adminError;

            if (string.IsNullOrWhiteSpace(request.Reason))
            {
                return StatusCode(StatusCodes.Status400BadRequest, new { message = "Reason is required" });
            }

            var targetUser = await this.Db.Users.FindAsync(userId);
            if (targetUser == null)
            {
                return StatusCode(StatusCodes.Status404NotFound, new { message = "User not found" });
            }

            var now = DateTime.UtcNow;

            // Check if already whitelisted
            var existingEntry = await this.Db.System_Waf_UserWhitelist
                .Where(w => w.UserId == userId && w.UnwhitelistedAtUtc == null && (w.ExpiresAtUtc == null || w.ExpiresAtUtc > now))
                .FirstOrDefaultAsync();

            if (existingEntry != null)
            {
                return StatusCode(StatusCodes.Status400BadRequest, new { message = "User is already whitelisted" });
            }

            await Waf.WhitelistUserAsync(
                user: targetUser,
                reason: request.Reason,
                adminUser: adminUser!,
                permanent: request.IsPermanent
            );

            var entry = await this.Db.System_Waf_UserWhitelist
                .Where(w => w.UserId == userId && w.UnwhitelistedAtUtc == null)
                .OrderByDescending(w => w.CreatedAtUtc)
                .FirstAsync();

            return StatusCode(StatusCodes.Status201Created, new WhitelistedUserItem
            {
                UserId = targetUser.Id,
                Reason = entry.JustificationForWhitelist,
                IsPermanent = entry.IsPermanent,
                IsActive = true,
                WhitelistedAt = entry.CreatedAtUtc,
                WhitelistedByUserId = entry.CreatedByUserId,
                ExpiresAt = entry.ExpiresAtUtc
            });
        }


        [HttpDelete("whitelist/users/{userId:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RemoveUserFromWhitelist(Guid userId, [FromQuery] string? reason = null)
        {
            var (adminUser, error) = await GetCurrentUserAsync();
            if (error != null) return error;
            var adminError = await this.RequireAdminAsync();
            if (adminError != null) return adminError;

            var targetUser = await this.Db.Users.FindAsync(userId);
            if (targetUser == null)
            {
                return StatusCode(StatusCodes.Status404NotFound, new { message = "User not found" });
            }

            var removed = await Waf.RemoveUserFromWhitelistAsync(targetUser, adminUser!);

            if (!removed)
            {
                return StatusCode(StatusCodes.Status404NotFound, new { message = "User is not currently whitelisted" });
            }

            return StatusCode(StatusCodes.Status204NoContent);
        }
        #endregion
    }
}
