using AuxiliumAPI.Common.EF;
using AuxiliumAPI.Common.EntityModels;
using AuxiliumAPI.Common.Enumerators;
using AuxiliumAPI.Common.Services.Interfaces;
using AuxiliumAPI.Common.Utilities;
using AuxiliumAPI.Models.Case;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AuxiliumAPI.Common.Services;

public class CaseDocumentService : ICaseDocumentService
{
    private readonly AuxiliumDbContext _db;
    private readonly ILogger<CaseDocumentService> _logger;

    public CaseDocumentService(
        AuxiliumDbContext db,
        ILogger<CaseDocumentService> logger)
    {
        _db = db;
        _logger = logger;
    }

    #region ========================= CASE DOCUMENT OPERATIONS =========================
    public async Task<CaseModel?> GetDocumentAsync(Guid caseId)
    {
        return await _db.Cases
            .Include(c => c.Workers)
            .Include(c => c.Clients)
            .Include(c => c.AdditionalProperties)
            .Include(c => c.Messages)
            .Include(c => c.Files)
            .Include(c => c.Todos)
            .FirstOrDefaultAsync(c => c.Id == caseId);
    }

    public async Task SaveDocumentAsync(CaseModel caseDoc)
    {
        caseDoc.LastUpdatedAt = DateTime.UtcNow;

        // check if the entity is tracked
        var entry = _db.Entry(caseDoc);
        if (entry.State == EntityState.Detached)
        {
            _db.Cases.Update(caseDoc);
        }

        await _db.SaveChangesAsync();
    }

    #endregion
    #region ========================= PEOPLE MANAGEMENT =========================
    public async Task AddClientAsync(Guid caseId, Guid userId)
    {
        try
        {
            // check if the relationship already exists
            var exists = await _db.CaseClients
                .AnyAsync(cc => cc.CaseId == caseId && cc.UserId == userId);

            if (exists)
            {
                _logger.LogInformation("Client {UserId} already assigned to case {CaseId}", userId, caseId);
                return;
            }

            // add the new client relationship to the case
            var caseClient = new CaseClientModel
            {
                Id = UUIDUtilities.GenerateV5(DatabaseObjectType.CaseClient),
                CaseId = caseId,
                UserId = userId,
                CreatedBy = userId, // TODO: pass through current user
                CreatedAt = DateTime.UtcNow
            };

            _db.CaseClients.Add(caseClient);

            // update the LastUpdatedAt timestamp for the case
            var caseEntity = await _db.Cases.FindAsync(caseId);
            if (caseEntity != null)
            {
                caseEntity.LastUpdatedAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();

            _logger.LogInformation("Added client {UserId} to case {CaseId}", userId, caseId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add client {UserId} to case {CaseId}", userId, caseId);
            throw;
        }
    }

    public async Task RemoveClientAsync(Guid caseId, Guid userId)
    {
        try
        {
            var caseClient = await _db.CaseClients
                .FirstOrDefaultAsync(cc => cc.CaseId == caseId && cc.UserId == userId);

            if (caseClient != null)
            {
                _db.CaseClients.Remove(caseClient);

                // update the LastUpdatedAt timestamp for the case
                var caseEntity = await _db.Cases.FindAsync(caseId);
                if (caseEntity != null)
                {
                    caseEntity.LastUpdatedAt = DateTime.UtcNow;
                }

                await _db.SaveChangesAsync();

                _logger.LogInformation("Removed client {UserId} from case {CaseId}", userId, caseId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove client {UserId} from case {CaseId}", userId, caseId);
            throw;
        }
    }

    public async Task AddWorkerAsync(Guid caseId, Guid userId)
    {
        try
        {
            // check if the relationship already exists
            var exists = await _db.CaseWorkers
                .AnyAsync(cw => cw.CaseId == caseId && cw.UserId == userId);

            if (exists)
            {
                _logger.LogInformation("Worker {UserId} already assigned to case {CaseId}", userId, caseId);
                return;
            }

            // add the new worker to the case
            var caseWorker = new CaseWorkerModel
            {
                Id = UUIDUtilities.GenerateV5(DatabaseObjectType.CaseWorker),
                CaseId = caseId,
                UserId = userId,
                CreatedBy = userId, // TODO: pass through current user
                CreatedAt = DateTime.UtcNow
            };

            _db.CaseWorkers.Add(caseWorker);

            // update the LastUpdatedAt timestamp for the case
            var caseEntity = await _db.Cases.FindAsync(caseId);
            if (caseEntity != null)
            {
                caseEntity.LastUpdatedAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();

            _logger.LogInformation("Added worker {UserId} to case {CaseId}", userId, caseId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add worker {UserId} to case {CaseId}", userId, caseId);
            throw;
        }
    }

    public async Task RemoveWorkerAsync(Guid caseId, Guid userId)
    {
        try
        {
            var caseWorker = await _db.CaseWorkers
                .FirstOrDefaultAsync(cw => cw.CaseId == caseId && cw.UserId == userId);

            if (caseWorker != null)
            {
                _db.CaseWorkers.Remove(caseWorker);

                // update the LastUpdatedAt timestamp for the case
                var caseEntity = await _db.Cases.FindAsync(caseId);
                if (caseEntity != null)
                {
                    caseEntity.LastUpdatedAt = DateTime.UtcNow;
                }

                await _db.SaveChangesAsync();

                _logger.LogInformation("Removed worker {UserId} from case {CaseId}", userId, caseId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove worker {UserId} from case {CaseId}", userId, caseId);
            throw;
        }
    }
    #endregion
    #region ========================= ADDITIONAL PROPERTIES =========================
    public async Task<List<CaseAdditionalPropertyModel>> GetAdditionalPropertiesAsync(Guid caseId)
    {
        try
        {
            return await _db.CaseAdditionalProperties
                .Where(a => a.CaseId == caseId)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get additional properties for case {CaseId}", caseId);
            throw;
        }
    }

    public async Task SaveAdditionalPropertyAsync(
        Guid caseId,
        string additionalPropertyName,
        string additionalPropertyContent
        )
    {
        try
        {
            // check if the property already exists
            var existing = await _db.CaseAdditionalProperties
                .FirstOrDefaultAsync(a => a.CaseId == caseId && a.Name == additionalPropertyName);

            if (existing != null)
            {
                throw new Exception($"Additional property {additionalPropertyName} already exists for case {caseId}");
            }
            else
            {
                // create the new additional property
                var newProperty = new CaseAdditionalPropertyModel
                {
                    Id = UUIDUtilities.GenerateV5(DatabaseObjectType.CaseAdditionalProperty),
                    CaseId = caseId,
                    ContentType = "text/plain",
                    CreatedBy = Guid.Empty, // TODO: Pass current user
                    CreatedAt = DateTime.UtcNow,
                    LastUpdatedAt = DateTime.UtcNow,
                    LastUpdatedBy = Guid.Empty,
                    Name = additionalPropertyName,
                    Content = additionalPropertyContent,
                };

                _db.CaseAdditionalProperties.Add(newProperty);
            }

            // update the LastUpdatedAt timestamp for the case
            var caseEntity = await _db.Cases.FindAsync(caseId);
            if (caseEntity != null)
            {
                caseEntity.LastUpdatedAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();

            _logger.LogInformation("Saved property {AdditionalPropertyName} for case {CaseId}", additionalPropertyName, caseId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save property {AdditionalPropertyName} for case {CaseId}", additionalPropertyName, caseId);
            throw;
        }
    }

    public async Task DeleteAdditionalPropertyAsync(Guid caseId, Guid additionalPropertyId)
    {
        try
        {
            var property = await _db.CaseAdditionalProperties
                .FirstOrDefaultAsync(a => a.CaseId == caseId && a.Id == additionalPropertyId);

            if (property != null)
            {
                _db.CaseAdditionalProperties.Remove(property);

                // update the LastUpdatedAt timestamp for the case
                var caseEntity = await _db.Cases.FindAsync(caseId);
                if (caseEntity != null)
                {
                    caseEntity.LastUpdatedAt = DateTime.UtcNow;
                }

                await _db.SaveChangesAsync();

                _logger.LogInformation("Deleted property {AdditionalPropertyId} from case {CaseId}", additionalPropertyId, caseId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete property {AdditionalPropertyId} from case {CaseId}", additionalPropertyId, caseId);
            throw;
        }
    }
    #endregion
    #region ========================= TODO OPERATIONS =========================
    public async Task<CaseTodoModel> CreateTodoAsync(
        Guid caseId,
        string summary,
        string? description,
        TodoPriorityEnum priority,
        Guid createdBy,
        DateTime? dueDate = null,
        Guid? assignedTo = null,
        DateTime? reminder = null)
    {
        try
        {
            // just make sure that the case actually exists
            var caseExists = await _db.Cases.AnyAsync(c => c.Id == caseId);
            if (!caseExists)
            {
                throw new KeyNotFoundException($"Case {caseId} not found");
            }

            // create the todo entity/model
            var todo = new CaseTodoModel
            {
                Id = UUIDUtilities.GenerateV5(DatabaseObjectType.CaseTodoItem),
                CaseId = caseId,
                Summary = summary,
                Description = description ?? string.Empty,
                Status = TodoStatusEnum.NeedsAction,
                Priority = priority,
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow,
                DueDate = dueDate,
                AssignedTo = assignedTo,
                Reminder = reminder
            };

            _db.CaseTodos.Add(todo);

            // update the LastUpdatedAt timestamp for the case
            var caseEntity = await _db.Cases.FindAsync(caseId);
            if (caseEntity != null)
            {
                caseEntity.LastUpdatedAt = DateTime.UtcNow;
                caseEntity.LastUpdatedBy = createdBy;
            }

            await _db.SaveChangesAsync();

            _logger.LogInformation("Created todo {TodoId} in case {CaseId}", todo.Id, caseId);

            return todo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create todo in case {CaseId}", caseId);
            throw;
        }
    }

    public async Task<CaseTodoModel?> GetTodoAsync(Guid caseId, Guid todoId)
    {
        try
        {
            return await _db.CaseTodos
                .Include(t => t.CreatedByUser)
                .Include(t => t.AssignedToUser)
                .Include(t => t.CompletedByUser)
                .FirstOrDefaultAsync(t => t.CaseId == caseId && t.Id == todoId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get todo {TodoId} from case {CaseId}", todoId, caseId);
            return null;
        }
    }

    public async Task<List<CaseTodoModel>> GetTodosAsync(Guid caseId)
    {
        try
        {
            return await _db.CaseTodos
                .Include(t => t.CreatedByUser)
                .Include(t => t.AssignedToUser)
                .Include(t => t.CompletedByUser)
                .Where(t => t.CaseId == caseId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get todos for case {CaseId}", caseId);
            throw;
        }
    }

    public async Task UpdateTodoStatusAsync(
        Guid caseId,
        Guid todoId,
        TodoStatusEnum newStatus,
        Guid? completedBy = null,
        string? completionNotes = null)
    {
        try
        {
            var todo = await _db.CaseTodos
                .FirstOrDefaultAsync(t => t.CaseId == caseId && t.Id == todoId)
                ?? throw new KeyNotFoundException($"Todo {todoId} not found in case {caseId}");

            todo.Status = newStatus;
            todo.LastUpdatedAt = DateTime.UtcNow;

            if (newStatus == TodoStatusEnum.Completed && completedBy != null)
            {
                todo.CompletedAt = DateTime.UtcNow;
                todo.CompletedBy = completedBy;
                todo.CompletionNote = completionNotes;
            }

            // update the LastUpdatedAt timestamp for the case
            var caseEntity = await _db.Cases.FindAsync(caseId);
            if (caseEntity != null)
            {
                caseEntity.LastUpdatedAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();

            _logger.LogInformation(
                "Updated todo {TodoId} status to {Status} in case {CaseId}",
                todoId, newStatus, caseId
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update todo {TodoId} status", todoId);
            throw;
        }
    }

    public async Task UpdateTodoAsync(
        Guid caseId,
        Guid todoId,
        string? summary = null,
        string? description = null,
        TodoPriorityEnum? priority = null,
        DateTime? dueDate = null,
        Guid? assignedTo = null,
        DateTime? reminder = null)
    {
        try
        {
            var todo = await _db.CaseTodos
                .FirstOrDefaultAsync(t => t.CaseId == caseId && t.Id == todoId)
                ?? throw new KeyNotFoundException($"Todo {todoId} not found in case {caseId}");

            if (summary != null) todo.Summary = summary;
            if (description != null) todo.Description = description;
            if (priority.HasValue) todo.Priority = priority.Value;
            if (dueDate.HasValue) todo.DueDate = dueDate;
            if (assignedTo.HasValue) todo.AssignedTo = assignedTo;
            if (reminder.HasValue) todo.Reminder = reminder;

            todo.LastUpdatedAt = DateTime.UtcNow;

            // update the LastUpdatedAt timestamp for the case
            var caseEntity = await _db.Cases.FindAsync(caseId);
            if (caseEntity != null)
            {
                caseEntity.LastUpdatedAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();

            _logger.LogInformation("Updated todo {TodoId} in case {CaseId}", todoId, caseId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update todo {TodoId}", todoId);
            throw;
        }
    }

    public async Task DeleteTodoAsync(Guid caseId, Guid todoId)
    {
        try
        {
            var todo = await _db.CaseTodos
                .FirstOrDefaultAsync(t => t.CaseId == caseId && t.Id == todoId)
                ?? throw new KeyNotFoundException($"Todo {todoId} not found in case {caseId}");

            _db.CaseTodos.Remove(todo);

            // update the LastUpdatedAt timestamp for the case
            var caseEntity = await _db.Cases.FindAsync(caseId);
            if (caseEntity != null)
            {
                caseEntity.LastUpdatedAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();

            _logger.LogInformation("Deleted todo {TodoId} from case {CaseId}", todoId, caseId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete todo {TodoId}", todoId);
            throw;
        }
    }
    #endregion
    #region ========================= PERMISSION CHECKS =========================
    public async Task<bool> CheckUserAccessAsync(Guid caseId, UserModel currentUser)
    {
        try
        {
            // admins have access to everything
            if (currentUser.IsAdmin) return true;

            // check whether user is client or worker
            var hasAccess = await _db.Cases
                .Where(c => c.Id == caseId)
                .AnyAsync(c =>
                    c.Clients!.Any(cl => cl.UserId == currentUser.Id) ||
                    c.Workers!.Any(w => w.UserId == currentUser.Id)
                );

            return hasAccess;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check access for user {UserId} on case {CaseId}", currentUser.Id, caseId);
            return false;
        }
    }
    #endregion
}
