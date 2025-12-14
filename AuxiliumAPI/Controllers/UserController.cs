using AuxiliumAPI.Common.ControllerBases;
using AuxiliumAPI.Common.CouchDbDocumentConstruction.Structures;
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
                sort: new[] { $"{{{sortBy}:\"{sortOrder}\"}}" }
            );

            var total = await _couchDb.CountAsync<UserDocumentStructure>(
                _configuration["Databases:CouchDB:Databases:Users"]!,
                selector
            );

            var totalPages = (int)Math.Ceiling(total / (double)pageSize);

            // build response models
            List<UserResponseModel> users;

            if (user.is_admin)
            {
                // admins get all the data
                users = result.Documents.Select(userDoc => new UserResponseModel
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

                    EmailAddress = "", // TODO: Add from MariaDB
                    IsAdmin = false,  // TODO: Add from MariaDB
                }).ToList();
            }
            else
            {
                // non-admins get a reduced amount of data
                users = result.Documents.Select(userDoc => new UserResponseModel
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
                }).ToList();
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
            return StatusCode(500, new { detail = $"Failed to fetch users: {ex.Message}" });
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

            // validate UUID
            if (!Guid.TryParse(userId, out _))
            {
                return BadRequest(new FailureResponseModel { Detail = "You must provide a valid UUID" });
            }

            // grab the user document from couchdb
            var userDoc = await _couchDb.GetDocumentAsync<UserDocumentStructure>(
                _configuration["Databases:CouchDB:Databases:Users"]!,
                userId
            );

            if (userDoc == null)
            {
                return NotFound(new FailureResponseModel { Detail = "User not found" });
            }

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

                    EmailAddress = "", // TODO: Add from MariaDB
                    IsAdmin = false,  // TODO: Add from MariaDB
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
            return StatusCode(500, new { detail = $"Failed to fetch user: {ex.Message}" });
        }
    }
}
