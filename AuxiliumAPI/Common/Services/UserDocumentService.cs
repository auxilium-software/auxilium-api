
using AuxiliumAPI.Common.EF;
using AuxiliumAPI.Common.EntityModels;
using AuxiliumAPI.Common.Enumerators;
using AuxiliumAPI.Common.Services.Interfaces;
using AuxiliumAPI.Common.Utilities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AuxiliumAPI.Common.Services;

public class UserDocumentService : IUserDocumentService
{
    private readonly AuxiliumDbContext _db;
    private readonly ILogger<UserDocumentService> _logger;

    public UserDocumentService(
        AuxiliumDbContext db,
        ILogger<UserDocumentService> logger)
    {
        _db = db;
        _logger = logger;
    }

    #region ========================= USER OPERATIONS =========================
    public async Task<UserModel?> GetDocumentAsync(Guid userId)
    {
        try
        {
            return await _db.Users
                .Include(u => u.AdditionalProperties)
                .FirstOrDefaultAsync(u => u.Id == userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user {UserId}", userId);
            return null;
        }
    }

    public async Task SaveDocumentAsync(UserModel userDoc)
    {
        try
        {
            userDoc.LastUpdatedAt = DateTime.UtcNow;

            var entry = _db.Entry(userDoc);
            if (entry.State == EntityState.Detached)
            {
                _db.Users.Update(userDoc);
            }

            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save user {UserId}", userDoc.Id);
            throw;
        }
    }
    #endregion
    #region ========================= ADDITIONAL PROPERTIES =========================
    public async Task<List<UserAdditionalPropertyModel>> GetAdditionalPropertiesAsync(Guid caseId)
    {
        try
        {
            return await _db.UserAdditionalProperties
                .Where(a => a.UserId == caseId)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get additional properties for case {CaseId}", caseId);
            throw;
        }
    }

    public async Task SaveAdditionalPropertyAsync(
        Guid userId,
        string additionalPropertyName,
        string additionalPropertyContent
        )
    {
        try
        {
            // check if the property exists
            var existing = await _db.UserAdditionalProperties
                .FirstOrDefaultAsync(a => a.UserId == userId && a.Name == additionalPropertyName);

            if (existing != null)
            {
                throw new Exception($"Additional property {additionalPropertyName} already exists for case {userId}");
            }
            else
            {
                // create the new property entity
                var newProperty = new UserAdditionalPropertyModel
                {
                    Id = UUIDUtilities.GenerateV5(DatabaseObjectType.UserAdditionalProperty),
                    UserId = userId,
                    ContentType = "text/plain",
                    CreatedBy = Guid.Empty, // TODO: Pass current user
                    CreatedAt = DateTime.UtcNow,
                    LastUpdatedAt = DateTime.UtcNow,
                    LastUpdatedBy = Guid.Empty,
                    Name = additionalPropertyName,
                    Content = additionalPropertyContent,
                };

                _db.UserAdditionalProperties.Add(newProperty);
            }

            // update the LastUpdatedAt timestamp for the case
            var userEntity = await _db.Users.FindAsync(userId);
            if (userEntity != null)
            {
                userEntity.LastUpdatedAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();

            _logger.LogInformation("Saved property {AdditionalPropertyName} for case {UserId}", additionalPropertyName, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save property {AdditionalPropertyName} for case {UserId}", additionalPropertyName, userId);
            throw;
        }
    }

    public async Task DeleteAdditionalPropertyAsync(Guid userId, Guid additionalPropertyId)
    {
        try
        {
            var property = await _db.UserAdditionalProperties
                .FirstOrDefaultAsync(a => a.UserId == userId && a.Id == additionalPropertyId);

            if (property != null)
            {
                _db.UserAdditionalProperties.Remove(property);

                // update the LastUpdatedAt timestamp for the case
                var userEntity = await _db.Users.FindAsync(userId);
                if (userEntity != null)
                {
                    userEntity.LastUpdatedAt = DateTime.UtcNow;
                }

                await _db.SaveChangesAsync();

                _logger.LogInformation("Deleted property {AdditionalPropertyId} from case {;}", additionalPropertyId, userId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete property {AdditionalPropertyId} from case {;}", additionalPropertyId, userId);
            throw;
        }
    }
    #endregion
    #region ========================= PERMISSION CHECKS =========================
    public async Task<bool> CheckUserAccessAsync(Guid userId, UserModel currentUser)
    {
        try
        {
            // admins can access absolutely everything
            if (currentUser.IsAdmin) return true;

            // users can access their own data
            if (currentUser.Id == userId) return true;

            // case workers can view other users (for assigning to cases)
            if (currentUser.IsCaseWorker) return true;

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check access for user {UserId}", userId);
            return false;
        }
    }
    #endregion
}
