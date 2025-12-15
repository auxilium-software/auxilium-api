using AuxiliumAPI.Common.ControllerBases;
using AuxiliumAPI.Common.DataStructures.CouchDB;
using AuxiliumAPI.Common.DataStructures.MariaDB;
using AuxiliumAPI.Common.Services.Interfaces;
using AuxiliumAPI.Models;
using AuxiliumAPI.Models.User;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AuxiliumAPI.Controllers;

[ApiController]
[Route("/api/v3/users")]
[Tags("Users")]
public class UserController : LoggedInControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<UserController> _logger;
    private readonly ICouchDbService _couchDb;

    public UserController(
        IConfiguration configuration,
        ILogger<UserController> logger,
        ICouchDbService couchDb,
        IMariaDbService mariaDb
        ) : base(mariaDb, logger)
    {
        _configuration = configuration;
        _logger = logger;
        _couchDb = couchDb;
    }

    [HttpGet("")]
    [ProducesResponseType(typeof(PaginatedUsersResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PaginatedUsersResponseModel>> SearchUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? sortBy = "createdAt",
        [FromQuery] string? sortOrder = "desc"
        )
    {
        try
        {
            // enforce login and get current user details
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            // check if user is admin
            object selector;

            if (user!.is_admin)
            {
                // admins see all users
                selector = new { };
            }
            else
            {
                //TODO: non-admins can only see users that share a case with them
                selector = new { };
            }

            // skip a "page"
            var skip = (page - 1) * pageSize;

            // query couchdb
            var result = await _couchDb.QueryAsync<UserDocumentStructure>(
                _configuration["Databases:CouchDB:Databases:Users"]!,
                selector,
                limit: pageSize,
                skip: skip,
                sort: [$"{{{sortBy}:\"{sortOrder}\"}}"]
            );

            var total = await _couchDb.CountAsync<UserDocumentStructure>(
                _configuration["Databases:CouchDB:Databases:Users"]!,
                selector
            );

            var totalPages = (int)Math.Ceiling(total / (double)pageSize);

            // get all the user ids from the result and feed it into a mariadb query
            var userIds = result.Documents.Select(d => d.Id).ToList();
            var mariaDbUsers = new Dictionary<string, UserRowStructure>();
            if (userIds.Count != 0)
            {
                var mariaDbData = await MariaDb.QueryAsync<UserRowStructure>(
                    "SELECT id, email_address, is_admin FROM users WHERE id IN @userIds",
                    new { userIds }
                );

                mariaDbUsers = mariaDbData.ToDictionary(
                    u => u.id.ToString(),
                    u => u
                );
            }

            // build response models
            List<UserResponseModel> users;

            if (user.is_admin)
            {
                // admins get all the data
                users = [.. result.Documents.Select(userDoc =>
                {
                    // get mariadb data for this user
                    mariaDbUsers.TryGetValue(userDoc.Id, out var mariaDbUser);

                    return new UserResponseModel
                    {
                        ID = Guid.Parse(userDoc.Id),
                        CreatedAt = userDoc.CreatedAt,
                        CreatedBy = userDoc.CreatedBy,
                        LastUpdatedAt = userDoc.LastUpdatedAt,
                        LastUpdatedBy = userDoc.LastUpdatedBy,

                        FullName = userDoc.FullName,
                        FullAddress = userDoc.FullAddress,
                        TelephoneNumber = userDoc.TelephoneNumber,
                        Gender = userDoc.Gender,
                        DateOfBirth = userDoc.DateOfBirth,

                        AdditionalProperties = userDoc.AdditionalProperties,
                        Files = userDoc.Files,

                        HowDidYouFindOutAboutOurService = userDoc.HowDidYouFindOutAboutOurService,

                        EmailAddress = mariaDbUser?.email_address ?? "[UNKNOWN]",
                        IsAdmin = mariaDbUser?.is_admin ?? false,
                    };
                })];
            }
            else
            {
                // non-admins get a reduced amount of data
                users = [.. result.Documents.Select(userDoc => new UserResponseModel
                {
                    ID = Guid.Parse(userDoc.Id),
                    CreatedAt = userDoc.CreatedAt,
                    CreatedBy = userDoc.CreatedBy,
                    LastUpdatedAt = null,
                    LastUpdatedBy = null,

                    FullName = userDoc.FullName,
                    FullAddress = "[REDACTED]",
                    TelephoneNumber = "[REDACTED]",
                    Gender = "[REDACTED]",
                    DateOfBirth = null,

                    AdditionalProperties = userDoc.AdditionalProperties,
                    Files = userDoc.Files,

                    HowDidYouFindOutAboutOurService = "[REDACTED]",

                    EmailAddress = "[REDACTED]",
                    IsAdmin = false,
                })];
            }

            var response = new PaginatedUsersResponseModel
            {
                Users = users,
                Total = total,
                Page = page,
                PerPage = pageSize,
                TotalPages = totalPages,
                HasMore = page < totalPages,
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search users");
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new FailureResponseModel { Detail = $"Failed to fetch users: {ex.Message}" }
            );
        }
    }

    [HttpGet("{userId}")]
    [ProducesResponseType(typeof(UserResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UserResponseModel>> GetUserById(string userId)
    {
        try
        {
            // enforce login and get current user details
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            // validate uuid
            if (!Guid.TryParse(userId, out _))
            {
                return BadRequest(new FailureResponseModel { Detail = "You must provide a valid UUID" });
            }

            // grab the user document from couchdb
            var userDoc = await _couchDb.GetDocumentAsync<UserDocumentStructure>(
                _configuration["Databases:CouchDB:Databases:Users"]!,
                Guid.Parse(userId)
            );

            if (userDoc == null)
            {
                return NotFound(new FailureResponseModel { Detail = "User not found" });
            }

            // fetch mariadb data for this user
            var mariaDbUser = await MariaDb.QuerySingleOrDefaultAsync<UserRowStructure>(
                "SELECT id, email_address, is_admin FROM users WHERE id = @userId",
                new { userId = Guid.Parse(userId) }
            );

            // build response model
            UserResponseModel response;

            if (user!.is_admin)
            {
                // admins get all the data
                response = new UserResponseModel
                {
                    ID = Guid.Parse(userDoc.Id),
                    CreatedAt = userDoc.CreatedAt,
                    CreatedBy = userDoc.CreatedBy,
                    LastUpdatedAt = userDoc.LastUpdatedAt,
                    LastUpdatedBy = userDoc.LastUpdatedBy,

                    FullName = userDoc.FullName,
                    FullAddress = userDoc.FullAddress,
                    TelephoneNumber = userDoc.TelephoneNumber,
                    Gender = userDoc.Gender,
                    DateOfBirth = userDoc.DateOfBirth,

                    AdditionalProperties = userDoc.AdditionalProperties,
                    Files = userDoc.Files,

                    HowDidYouFindOutAboutOurService = userDoc.HowDidYouFindOutAboutOurService,

                    EmailAddress = mariaDbUser?.email_address ?? "[UNKNOWN]",
                    IsAdmin = mariaDbUser?.is_admin ?? false,
                };
            }
            else
            {
                // non-admins get a reduced amount of data
                response = new UserResponseModel
                {
                    ID = Guid.Parse(userDoc.Id),
                    CreatedAt = userDoc.CreatedAt,
                    CreatedBy = userDoc.CreatedBy,
                    LastUpdatedAt = null,
                    LastUpdatedBy = null,

                    FullName = userDoc.FullName,
                    FullAddress = "[REDACTED]",
                    TelephoneNumber = "[REDACTED]",
                    Gender = "[REDACTED]",
                    DateOfBirth = null,

                    AdditionalProperties = userDoc.AdditionalProperties,
                    Files = userDoc.Files,

                    HowDidYouFindOutAboutOurService = "[REDACTED]",

                    EmailAddress = "[REDACTED]",
                    IsAdmin = false,
                };
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch user {UserId}", userId);
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new FailureResponseModel { Detail = $"Failed to fetch user: {ex.Message}" }
            );
        }
    }
}
