using AuxiliumSoftware.AuxiliumServices.API.Common.ControllerBases;
using AuxiliumSoftware.AuxiliumServices.API.Common.Utilities;
using AuxiliumSoftware.AuxiliumServices.API.Models;
using AuxiliumSoftware.AuxiliumServices.API.Models.User;
using AuxiliumSoftware.AuxiliumServices.API.Models.UserStatistic;
using AuxiliumSoftware.AuxiliumServices.Common.Configuration;
using AuxiliumSoftware.AuxiliumServices.Common.DataStructures;
using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework;
using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework.EntityModels;
using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework.Enumerators;
using AuxiliumSoftware.AuxiliumServices.Common.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuxiliumSoftware.AuxiliumServices.API.Controllers;

[ApiController]
[Route("/api/v3/users")]
[Tags("Users")]
public class UserController : LoggedInControllerBase
{
    private readonly IUserDocumentService _userDocService;

    public UserController(
        ISystemSettingsService systemSettingsService,
        IConfiguration configuration,
        AuxiliumDbContext db,
        IWafService waf,
        ILogger<UserController> logger,
        ITotpService totpService,

        IUserDocumentService userDocService
        )
        : base(systemSettingsService, configuration, db, waf, logger, totpService)
    {
        _userDocService = userDocService;
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

            if (user!.IsAdmin)
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
                if (user.IsAdmin)
                {
                    // admins get all data including additional properties
                    var additionalProperties = userDoc.AdditionalProperties?
                        .ToDictionary(
                            p => p.UrlSlug,
                            p => new AdditionalPropertySubStructure
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
                        ) ?? new Dictionary<string, AdditionalPropertySubStructure>();

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
                        IsAdmin = userDoc.IsAdmin,
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

                        AdditionalProperties = new Dictionary<string, AdditionalPropertySubStructure>(),
                        Files = new List<string>(),

                        HowDidYouFindOutAboutOurService = "[REDACTED]",

                        IsEmailVerified = null,
                        AllowLogin = null,
                        IsAdmin = null,
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
            var adminCount = await Db.Users.CountAsync(u => u.IsAdmin);
            var caseWorkerCount = await Db.Users.CountAsync(u => u.IsCaseWorker);
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
                    RegularUsers = regularUserCount
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
}
