using AuxiliumAPI.Common.ControllerBases;
using AuxiliumAPI.Models;
using AuxiliumAPI.Models.User;
using AuxiliumSoftware.AuxiliumServices.Common.Configuration;
using AuxiliumSoftware.AuxiliumServices.Common.DataStructures;
using AuxiliumSoftware.AuxiliumServices.Common.EF;
using AuxiliumSoftware.AuxiliumServices.Common.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuxiliumAPI.Controllers;

[ApiController]
[Route("/api/v3/users")]
[Tags("Users")]
public class UserController : LoggedInControllerBase
{
    private readonly ILogger<UserController> _logger;

    private readonly IUserDocumentService _userDocService;

    public UserController(
        ILogger<UserController> logger,
        IUserDocumentService userDocService,
        AuxiliumDbContext db
        )
        : base(db, logger)
    {
        _logger = logger;

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
        [FromQuery] string? search = null)
    {
        try
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            IQueryable<AuxiliumSoftware.AuxiliumServices.Common.EntityModels.UserModel> query;

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
                        Db.CaseClients.Any(cc => cc.UserId == user.Id &&
                            Db.CaseClients.Any(cc2 => cc2.CaseId == cc.CaseId && cc2.UserId == u.Id)) ||
                        // users they're a worker with
                        Db.CaseWorkers.Any(cw => cw.UserId == user.Id &&
                            Db.CaseWorkers.Any(cw2 => cw2.CaseId == cw.CaseId && cw2.UserId == u.Id)) ||
                        // or themselves
                        u.Id == user.Id
                    )
                    .Distinct();
            }

            // apply search filter
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(u =>
                    u.FullName != null && u.FullName.Contains(search) ||
                    u.EmailAddress.Contains(search)
                );
            }

            // apply sorting
            query = ApplySorting(query, sortBy, sortOrder);

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
                            p => p.Name,
                            p => new AdditionalPropertySubStructure
                            {
                                Id = p.Id,
                                CreatedAt = p.CreatedAt,
                                CreatedBy = p.CreatedBy,
                                UpdatedAt = p.LastUpdatedAt,
                                LastUpdatedBy = p.LastUpdatedBy,
                                OriginalName = p.Name,
                                PrettyName = p.Name,
                                UrlSlug = p.Name.ToLower().Replace(" ", "-"),
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

                        FullName = userDoc.FullName ?? string.Empty,
                        FullAddress = userDoc.FullAddress ?? string.Empty,
                        TelephoneNumber = userDoc.TelephoneNumber ?? string.Empty,
                        Gender = userDoc.Gender ?? string.Empty,
                        DateOfBirth = userDoc.DateOfBirth,

                        AdditionalProperties = additionalProperties,
                        Files = new List<string>(),

                        HowDidYouFindOutAboutOurService = userDoc.HowDidYouFindOutAboutOurService ?? string.Empty,

                        EmailAddress = userDoc.EmailAddress,
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

                        FullName = userDoc.FullName ?? string.Empty,
                        FullAddress = "[REDACTED]",
                        TelephoneNumber = "[REDACTED]",
                        Gender = "[REDACTED]",
                        DateOfBirth = null,

                        AdditionalProperties = new Dictionary<string, AdditionalPropertySubStructure>(),
                        Files = new List<string>(),

                        HowDidYouFindOutAboutOurService = "[REDACTED]",

                        EmailAddress = "[REDACTED]",
                        IsAdmin = false,
                        IsCaseWorker = false
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
            _logger.LogError(ex, "Failed to search users");
            return StatusCode(500, new FailureResponseModel
            {
                Detail = "Failed to fetch users"
            });
        }
    }

    [HttpGet("{userId:guid}")]
    [ProducesResponseType(typeof(UserResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UserResponseModel>> GetUserById(Guid userId)
    {
        try
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            var userDoc = await Db.Users
                .Include(u => u.AdditionalProperties)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (userDoc == null)
            {
                return NotFound(new FailureResponseModel { Detail = "User not found" });
            }

            UserResponseModel response;

            if (user!.IsAdmin)
            {
                // admins get all data including additional properties
                var additionalProperties = userDoc.AdditionalProperties?
                    .ToDictionary(
                        p => p.Name,
                        p => new AdditionalPropertySubStructure
                        {
                            Id = p.Id,
                            CreatedAt = p.CreatedAt,
                            CreatedBy = p.CreatedBy,
                            UpdatedAt = p.LastUpdatedAt,
                            LastUpdatedBy = p.LastUpdatedBy,
                            OriginalName = p.Name,
                            PrettyName = p.Name,
                            UrlSlug = p.Name.ToLower().Replace(" ", "-"),
                            Content = p.Content,
                            ContentType = p.ContentType
                        }
                    ) ?? new Dictionary<string, AdditionalPropertySubStructure>();

                response = new UserResponseModel
                {
                    ID = userDoc.Id,
                    CreatedAt = userDoc.CreatedAt,
                    CreatedBy = userDoc.CreatedBy,
                    LastUpdatedAt = userDoc.LastUpdatedAt,
                    LastUpdatedBy = userDoc.LastUpdatedBy,

                    FullName = userDoc.FullName ?? string.Empty,
                    FullAddress = userDoc.FullAddress ?? string.Empty,
                    TelephoneNumber = userDoc.TelephoneNumber ?? string.Empty,
                    Gender = userDoc.Gender ?? string.Empty,
                    DateOfBirth = userDoc.DateOfBirth,

                    AdditionalProperties = additionalProperties,
                    Files = new List<string>(),

                    HowDidYouFindOutAboutOurService = userDoc.HowDidYouFindOutAboutOurService ?? string.Empty,

                    EmailAddress = userDoc.EmailAddress,
                    IsAdmin = userDoc.IsAdmin,
                    IsCaseWorker = userDoc.IsCaseWorker
                };
            }
            else
            {
                // non-admins get redacted data
                response = new UserResponseModel
                {
                    ID = userDoc.Id,
                    CreatedAt = userDoc.CreatedAt,
                    CreatedBy = userDoc.CreatedBy,
                    LastUpdatedAt = null,
                    LastUpdatedBy = null,

                    FullName = userDoc.FullName ?? string.Empty,
                    FullAddress = "[REDACTED]",
                    TelephoneNumber = "[REDACTED]",
                    Gender = "[REDACTED]",
                    DateOfBirth = null,

                    AdditionalProperties = new Dictionary<string, AdditionalPropertySubStructure>(),
                    Files = new List<string>(),

                    HowDidYouFindOutAboutOurService = "[REDACTED]",

                    EmailAddress = "[REDACTED]",
                    IsAdmin = false,
                    IsCaseWorker = false
                };
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch user {UserId}", userId);
            return StatusCode(500, new FailureResponseModel
            {
                Detail = "Failed to fetch user"
            });
        }
    }

    // helper method for sorting
    private IQueryable<AuxiliumSoftware.AuxiliumServices.Common.EntityModels.UserModel> ApplySorting(
        IQueryable<AuxiliumSoftware.AuxiliumServices.Common.EntityModels.UserModel> query,
        string? sortBy,
        string? sortOrder)
    {
        var descending = sortOrder?.ToLower() == "desc";

        return sortBy?.ToLower() switch
        {
            "createdat" => descending
                ? query.OrderByDescending(u => u.CreatedAt)
                : query.OrderBy(u => u.CreatedAt),
            "fullname" => descending
                ? query.OrderByDescending(u => u.FullName)
                : query.OrderBy(u => u.FullName),
            "email" => descending
                ? query.OrderByDescending(u => u.EmailAddress)
                : query.OrderBy(u => u.EmailAddress),
            _ => query.OrderByDescending(u => u.CreatedAt)
        };
    }
}
