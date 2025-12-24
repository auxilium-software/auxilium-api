using AuxiliumAPI.Common.EF;
using AuxiliumAPI.Common.EntityModels;
using AuxiliumAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AuxiliumAPI.Common.ControllerBases;

[Authorize]
public abstract class LoggedInControllerBase : ControllerBase
{
    protected readonly AuxiliumDbContext Db;
    protected readonly ILogger Logger;

    protected LoggedInControllerBase(AuxiliumDbContext db, ILogger logger)
    {
        Db = db;
        Logger = logger;
    }

    protected async Task<(UserModel? user, ActionResult? error)> GetCurrentUserAsync()
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

    protected (Guid? userId, ActionResult? error) GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim))
        {
            return (null, Unauthorized(new FailureResponseModel
            {
                Detail = "User ID not found in token"
            }));
        }

        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return (null, BadRequest(new FailureResponseModel
            {
                Detail = "Invalid user ID format"
            }));
        }

        return (userId, null);
    }

    protected async Task<bool> IsCurrentUserAdminAsync()
    {
        var (user, _) = await GetCurrentUserAsync();
        return user?.IsAdmin ?? false;
    }

    protected async Task<bool> IsCurrentUserCaseWorkerAsync()
    {
        var (user, _) = await GetCurrentUserAsync();
        return user?.IsCaseWorker ?? false;
    }

    protected async Task<(UserModel? user, ActionResult? error)> RequireAdminAsync()
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

    protected async Task<(UserModel? user, ActionResult? error)> RequireCaseWorkerAsync()
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
