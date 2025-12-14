using AuxiliumAPI.Common.DataStructures.MariaDB;
using AuxiliumAPI.Common.Services.Interfaces;
using AuxiliumAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AuxiliumAPI.Common.ControllerBases
{
    [Authorize]
    public abstract class LoggedInControllerBase : ControllerBase
    {
        protected readonly IMariaDbService MariaDb;
        protected readonly ILogger Logger;

        protected LoggedInControllerBase(IMariaDbService mariaDb, ILogger logger)
        {
            MariaDb = mariaDb;
            Logger = logger;
        }

        protected async Task<(UserRowStructure? user, ActionResult? error)> GetCurrentUserAsync()
        {
            // get user id from jwt
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return (null, Unauthorized(new FailureResponseModel
                {
                    Detail = "User ID not found in token"
                }));
            }

            if (!Guid.TryParse(userId, out _))
            {
                return (null, BadRequest(new FailureResponseModel
                {
                    Detail = "Invalid user ID format"
                }));
            }

            // grab user from database
            var user = await MariaDb.QuerySingleOrDefaultAsync<UserRowStructure>(
                "SELECT * FROM users WHERE id = @userId",
                new { userId = Guid.Parse(userId) }
            );

            if (user == null)
            {
                return (null, Unauthorized(new FailureResponseModel
                {
                    Detail = "User not found in database"
                }));
            }

            return (user, null);
        }
    }
}
