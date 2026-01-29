using AuxiliumSoftware.AuxiliumServices.API.Models;
using AuxiliumSoftware.AuxiliumServices.Common.Configuration;
using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework;
using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework.EntityModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AuxiliumSoftware.AuxiliumServices.API.Common.ControllerBases
{
    /// <summary>
    /// This is an inheritable class for controllers that require the user to be logged in.
    /// </summary>
    [Authorize]
    public abstract class LoggedInControllerBase : ControllerBase
    {
        protected readonly AuxiliumDbContext Db;
        protected readonly ILogger Logger;
        protected readonly ConfigurationStructure Configuration;

        /**
         * Constructor for LoggedInControllerBase
         * 
         * @param db The database context to use for data access.
         * @param logger The logger instance for logging.
         */
        protected LoggedInControllerBase(
            IConfiguration configuration,
            AuxiliumDbContext db,
            ILogger logger
        )
        {
            this.Configuration = configuration.Get<ConfigurationStructure>()!;
            this.Db = db;
            this.Logger = logger;
        }

        /**
         * Grabs the currently logged in user using the JWT provided in the Bearer auth,
         * makes sure that the ID is there and a valid UUID,
         * and whether it exists in the database
         * 
         * @return The user model of the currently logged in user, or an error ActionResult if something went wrong.
         */
        protected async Task<(UserEntityModel? user, ActionResult? error)> GetCurrentUserAsync()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
            {
                Logger.LogWarning("User ID not found in JWT token");
                return (null, Unauthorized(new FailureResponseModel
                {
                    Detail = "User ID not found in token"
                }));
            }

            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                Logger.LogWarning("Invalid user ID format in token: {UserId}", userIdClaim);
                return (null, BadRequest(new FailureResponseModel
                {
                    Detail = "Invalid user ID format"
                }));
            }

            var user = await Db.Users
                .AsNoTracking()  // read-only query for better performance
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                Logger.LogWarning("User {UserId} not found in database", userId);
                return (null, Unauthorized(new FailureResponseModel
                {
                    Detail = "User not found in database"
                }));
            }

            return (user, null);
        }

        /**
         * Ensures that the currently logged in user has admin privileges.
         * 
         * @return The user model of the currently logged in admin user, or an error ActionResult if something went wrong.
         */
        protected async Task<(UserEntityModel? user, ActionResult? error)> RequireAdminAsync()
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return (null, error);

            if (!user!.IsAdmin)
            {
                Logger.LogWarning("User {UserId} attempted admin action without permissions", user.Id);
                return (null, StatusCode(403, new FailureResponseModel
                {
                    Detail = "Administrator privileges required"
                }));
            }

            return (user, null);
        }

        /**
         * Ensures that the currently logged in user has case worker or admin privileges.
         * 
         * @return The user model of the currently logged in case worker or admin user, or an error ActionResult if something went wrong.
         */
        protected async Task<(UserEntityModel? user, ActionResult? error)> RequireCaseWorkerAsync()
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return (null, error);

            if (!user!.IsCaseWorker && !user.IsAdmin)
            {
                Logger.LogWarning("User {UserId} attempted case worker action without permissions", user.Id);
                return (null, StatusCode(403, new FailureResponseModel
                {
                    Detail = "Case worker or administrator privileges required"
                }));
            }

            return (user, null);
        }
    }
}
