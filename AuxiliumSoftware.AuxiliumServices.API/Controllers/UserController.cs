using AuxiliumSoftware.AuxiliumServices.API.Common.ControllerBases;
using AuxiliumSoftware.AuxiliumServices.API.Common.Utilities;
using AuxiliumSoftware.AuxiliumServices.API.Models;
using AuxiliumSoftware.AuxiliumServices.API.Models.User;
using AuxiliumSoftware.AuxiliumServices.API.Models.UserStatistic;
using AuxiliumSoftware.AuxiliumServices.Common.Configuration;
using AuxiliumSoftware.AuxiliumServices.Common.DataTransferObjects;
using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework;
using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework.EntityModels;
using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework.Enumerators;
using AuxiliumSoftware.AuxiliumServices.Common.Enumerators;
using AuxiliumSoftware.AuxiliumServices.Common.Messaging.Interfaces;
using AuxiliumSoftware.AuxiliumServices.Common.Messaging.Models;
using AuxiliumSoftware.AuxiliumServices.Common.Messaging.Models.Enumerators;
using AuxiliumSoftware.AuxiliumServices.Common.Services;
using AuxiliumSoftware.AuxiliumServices.Common.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace AuxiliumSoftware.AuxiliumServices.API.Controllers;

[ApiController]
[Route("/api/v3/users")]
[Tags("Users")]
public class UserController : LoggedInControllerBase
{
    private readonly IUserDocumentService _userDocService;
    private readonly IMessageQueueProducer _messageQueue;

    public UserController(
        ISystemSettingsService systemSettingsService,
        IConfiguration configuration,
        AuxiliumDbContext db,
        IWebApplicationFirewallService waf,
        ILogger<UserController> logger,
        ITotpService totpService,

        IUserDocumentService userDocService,
        IMessageQueueProducer messageQueue
        )
        : base(systemSettingsService, configuration, db, waf, logger, totpService)
    {
        _userDocService = userDocService;
        _messageQueue = messageQueue;
    }

    [HttpGet("")]
    [ProducesResponseType(typeof(PaginatedUsersResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PaginatedUsersResponseModel>> SearchUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? sortBy = "createdAt",
        [FromQuery] string? sortOrder = "desc",
        [FromQuery] string? search = null
        )
    {
        try
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            IQueryable<UserEntityModel> query;

            if (user!.IsAdministrator)
            {
                // admins see all users
                query = Db.Users.Include(u => u.AdditionalProperties);
            }
            else
            {
                // non-admins see users they share cases with
                query = Db.Users
                    .Include(u => u.AdditionalProperties)
                    .Where(u =>
                        // users they're a client with
                        Db.CaseClients.Any(
                            cc => cc.UserId == user.Id
                            && Db.CaseClients.Any(
                                cc2 => cc2.CaseId == cc.CaseId
                                && cc2.UserId == u.Id
                            )
                        )
                        // -OR- users they're a worker with
                        || Db.CaseWorkers.Any(
                            cw => cw.UserId == user.Id
                            && Db.CaseWorkers.Any(
                                cw2 => cw2.CaseId == cw.CaseId
                                && cw2.UserId == u.Id
                            )
                        )
                        // -OR- themselves
                        || u.Id == user.Id
                    )
                    .Distinct();
            }

            // apply search filter
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(u =>
                    (u.FullName != null && u.FullName.Contains(search))
                    || u.EmailAddress.Contains(search)
                );
            }

            // apply sorting
            query = ControllerUtilities.ApplySortingForUsers(query, sortBy, sortOrder);

            var total = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(total / (double)pageSize);

            var users = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // build response models
            var userResponses = new List<UserResponseModel>();

            foreach (var userDoc in users)
            {
                if (user.IsAdministrator)
                {
                    // admins get all data including additional properties
                    var additionalProperties = userDoc.AdditionalProperties?
                        .ToDictionary(
                            p => p.UrlSlug,
                            p => new AdditionalPropertySubStructureDTO
                            {
                                Id = p.Id,
                                CreatedAt = p.CreatedAt,
                                CreatedBy = p.CreatedBy,
                                UpdatedAt = p.LastUpdatedAt,
                                LastUpdatedBy = p.LastUpdatedBy,
                                OriginalName = p.OriginalName,
                                UrlSlug = p.UrlSlug,
                                Content = p.Content,
                                ContentType = p.ContentType
                            }
                        ) ?? new Dictionary<string, AdditionalPropertySubStructureDTO>();

                    userResponses.Add(new UserResponseModel
                    {
                        ID = userDoc.Id,
                        CreatedAt = userDoc.CreatedAt,
                        CreatedBy = userDoc.CreatedBy,
                        LastUpdatedAt = userDoc.LastUpdatedAt,
                        LastUpdatedBy = userDoc.LastUpdatedBy,

                        EmailAddress = userDoc.EmailAddress,
                        FullName = userDoc.FullName ?? string.Empty,
                        FullAddress = userDoc.FullAddress ?? string.Empty,
                        TelephoneNumber = userDoc.TelephoneNumber ?? string.Empty,
                        Gender = userDoc.Gender ?? string.Empty,
                        DateOfBirth = userDoc.DateOfBirth,
                        LanguagePreference = userDoc.LanguagePreference,

                        AdditionalProperties = additionalProperties,
                        Files = new List<string>(),

                        HowDidYouFindOutAboutOurService = userDoc.HowDidYouFindOutAboutOurService ?? string.Empty,

                        IsEmailVerified = userDoc.HasEmailAddressBeenVerified,
                        AllowLogin = userDoc.AllowLogin,
                        IsAdministrator = userDoc.IsAdministrator,
                        IsCaseWorkerManager = userDoc.IsCaseWorkerManager,
                        IsCaseWorker = userDoc.IsCaseWorker
                    });
                }
                else
                {
                    // non-admins get redacted data
                    userResponses.Add(new UserResponseModel
                    {
                        ID = userDoc.Id,
                        CreatedAt = userDoc.CreatedAt,
                        CreatedBy = userDoc.CreatedBy,
                        LastUpdatedAt = null,
                        LastUpdatedBy = null,

                        EmailAddress = "[REDACTED]",
                        FullName = userDoc.FullName ?? string.Empty,
                        FullAddress = "[REDACTED]",
                        TelephoneNumber = "[REDACTED]",
                        Gender = "[REDACTED]",
                        DateOfBirth = null,
                        LanguagePreference = "[REDACTED]",

                        AdditionalProperties = new Dictionary<string, AdditionalPropertySubStructureDTO>(),
                        Files = new List<string>(),

                        HowDidYouFindOutAboutOurService = "[REDACTED]",

                        IsEmailVerified = null,
                        AllowLogin = null,
                        IsAdministrator = null,
                        IsCaseWorkerManager = null,
                        IsCaseWorker = null
                    });
                }
            }

            var response = new PaginatedUsersResponseModel
            {
                Users = userResponses,
                Total = total,
                Page = page,
                PerPage = pageSize,
                TotalPages = totalPages,
                HasMore = page < totalPages
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Failed to search users");
            return StatusCode(500, new FailureResponseModel
            {
                Detail = "Failed to fetch users"
            });
        }
    }

    [ApiExplorerSettings(IgnoreApi = true)]
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(UserStatisticsResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UserStatisticsResponseModel>> GetUserStatistics(
        [FromQuery] string period = "week"
    )
    {
        try
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            var adminError = await RequireAdminAsync();
            if (adminError != null) return adminError;

            var now = DateTime.UtcNow;
            var totalUsers = await Db.Users.CountAsync();

            // calculate growth percentage from last month
            var startOfThisMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var startOfLastMonth = startOfThisMonth.AddMonths(-1);

            var usersCreatedThisMonth = await Db.Users
                .CountAsync(u => u.CreatedAt >= startOfThisMonth);
            var usersCreatedLastMonth = await Db.Users
                .CountAsync(u => u.CreatedAt >= startOfLastMonth && u.CreatedAt < startOfThisMonth);

            double growthPercentage = 0;
            if (usersCreatedLastMonth > 0)
            {
                growthPercentage = Math.Round(
                    ((double)(usersCreatedThisMonth - usersCreatedLastMonth) / usersCreatedLastMonth) * 100,
                    1
                );
            }


            var growthData = await UserStatisticsCalculationUtilities.GetUserGrowthData(this.Db, period, now);
            var languageDistribution = await UserStatisticsCalculationUtilities.GetLanguageDistributionData(this.Db);


            // additional stats
            var adminCount = await Db.Users.CountAsync(u => u.IsAdministrator);
            var caseWorkerCount = await Db.Users.CountAsync(u => u.IsCaseWorker);
            var caseWorkerManagerCount = await Db.Users.CountAsync(u => u.IsCaseWorkerManager);
            var regularUserCount = totalUsers - adminCount - caseWorkerCount;

            var response = new UserStatisticsResponseModel
            {
                TotalUsers = totalUsers,
                GrowthPercentage = growthPercentage,
                UsersCreatedThisMonth = usersCreatedThisMonth,
                UsersCreatedLastMonth = usersCreatedLastMonth,

                GrowthChart = growthData,
                LanguageDistribution = languageDistribution,

                UserTypeBreakdown = new UserTypeBreakdownModel
                {
                    Admins = adminCount,
                    CaseWorkers = caseWorkerCount,
                    CaseWorkerManagers = caseWorkerManagerCount,
                    RegularUsers = regularUserCount,
                },

                GeneratedAt = now
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to fetch user statistics");
            return StatusCode(500, new FailureResponseModel
            {
                Detail = "Failed to fetch user statistics"
            });
        }
    }









    [HttpPost("")]
    [ProducesResponseType(typeof(UserResponseModel), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UserResponseModel>> CreateUser(
    [FromBody] AdminCreateUserRequestModel request)
    {
        try
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            var adminError = await RequireAdminAsync();
            if (adminError != null) return adminError;

            var totpError = await RequireTotpAsync(user!.Id);
            if (totpError != null) return totpError;

            // check for existing email
            var emailExists = await Db.Users
                .AnyAsync(u => u.EmailAddress == request.EmailAddress);

            if (emailExists)
            {
                return Conflict(new FailureResponseModel
                {
                    Detail = "A user with this email address already exists"
                });
            }

            var userId = UUIDUtilities.GenerateV5(DatabaseObjectTypeEnum.User);

            var newUser = new UserEntityModel
            {
                Id = userId,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = user.Id,
                EmailAddress = request.EmailAddress,
                FullName = request.FullName,
                FullAddress = "",
                TelephoneNumber = "",
                Gender = "",
                DateOfBirth = new DateOnly(),
                LanguagePreference = request.LanguagePreference,
                PasswordHash = string.Empty, // no password yet — set via token
                AllowLogin = false,          // locked until password is set
                MustChangePassword = true,
                HasEmailAddressBeenVerified = false,
                IsAdministrator = false,
                IsCaseWorker = false,
                IsCaseWorkerManager = false,
                HowDidYouFindOutAboutOurService = ""
            };

            Db.Users.Add(newUser);

            // generate password-set token
            var rawTokenBytes = RandomNumberGenerator.GetBytes(32);
            var rawToken = Convert.ToBase64String(rawTokenBytes);
            var tokenHash = Convert.ToBase64String(SHA256.HashData(rawTokenBytes));

            var passwordToken = new PasswordSetTokenEntityModel
            {
                Id = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,
                CreatedBy = user.Id,
                UserId = userId,
                TokenHash = tokenHash,
                ExpiresAt = DateTime.UtcNow.AddHours(72),
                UsedAt = null,
                Reason = PasswordSetTokenReasonEnum.NewAccount,
            };

            Db.PasswordSetTokens.Add(passwordToken);
            await Db.SaveChangesAsync();

            // send the welcome/password-set email
            var portalBaseUrl = await SystemSettings.GetStringAsync(SystemSettingKeyEnum.Instance_Navigation_PortalBaseUrl);
            await _messageQueue.PublishAsync(new EmailQueueMessage
            {
                TargetUserId = newUser.Id,
                Subject = "Your Account Has Been Created",
                TemplateName = "account-created",
                Priority = EmailPriorityEnum.High,
                TemplateData = new Dictionary<string, string>
                {
                    ["reset_link"] = $"{portalBaseUrl}/set-initial-password?token={Uri.EscapeDataString(rawToken)}",
                    ["expiry_hours"] = "72",
                }
            });

            await _userDocService.WriteToAuditLog(
                currentUser: user,
                targetUser: newUser,
                entityType: UserEntityTypeEnum.User,
                entityId: newUser.Id,
                actionType: AuditLogActionTypeEnum.Creation
            );

            Logger.LogInformation(
                "User {NewUserId} ({Email}) created by admin {AdminId}",
                userId, newUser.EmailAddress, user.Id
            );

            return CreatedAtAction(
                nameof(CreateUser),
                new { userId },
                ControllerUtilities.UserMapToUserResponseModel(newUser, true)
            );
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to create user");
            return StatusCode(500, new FailureResponseModel
            {
                Detail = "Failed to create user"
            });
        }
    }
}
