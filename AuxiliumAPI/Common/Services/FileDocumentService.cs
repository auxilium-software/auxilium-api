using AuxiliumAPI.Common.EF;
using AuxiliumAPI.Common.EntityModels;
using AuxiliumAPI.Common.Enumerators;
using AuxiliumAPI.Common.Services.Interfaces;
using AuxiliumAPI.Common.Utilities;
using Microsoft.EntityFrameworkCore;

namespace AuxiliumAPI.Common.Services;

public class FileDocumentService : IFileDocumentService
{
    private readonly AuxiliumDbContext _db;
    private readonly IConfiguration _configuration;
    private readonly ILogger<FileDocumentService> _logger;
    private readonly string _lfsBasePath;

    public FileDocumentService(
        AuxiliumDbContext db,
        IConfiguration configuration,
        ILogger<FileDocumentService> logger)
    {
        _db = db;
        _configuration = configuration;
        _logger = logger;

        _lfsBasePath = _configuration["FileSystem:RootStorageDirectories:AuxLFS"]!;
    }

    #region case file operations
    public async Task<CaseFileModel?> GetCaseFileMetadataAsync(Guid fileId)
    {
        try
        {
            return await _db.CaseFiles
                .Include(f => f.Case)
                .Include(f => f.CreatedByUser)
                .FirstOrDefaultAsync(f => f.Id == fileId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get case file metadata for {FileId}", fileId);
            return null;
        }
    }

    public async Task<List<CaseFileModel>> GetFilesForCaseAsync(Guid caseId)
    {
        try
        {
            return await _db.CaseFiles
                .Include(f => f.CreatedByUser)
                .Where(f => f.CaseId == caseId)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get files for case {CaseId}", caseId);
            throw;
        }
    }

    public async Task<(string uri, CaseFileModel metadata)> SaveCaseFileAsync(
        byte[] fileContent,
        string filename,
        string contentType,
        Guid uploadedBy,
        Guid caseId,
        string? description = null)
    {
        try
        {
            var fileId = UUIDUtilities.GenerateV5(DatabaseObjectType.File);
            var filePath = Path.Combine(_lfsBasePath, $"{fileId}.bin");

            // just make sure the directory actually exists
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // write the file to lfs
            await File.WriteAllBytesAsync(filePath, fileContent);
            var hash = HashingUtilities.SHA256Hash(fileContent);

            // create metadata
            var fileMetadata = new CaseFileModel
            {
                Id = fileId,
                CaseId = caseId,
                Filename = filename,
                ContentType = contentType,
                Size = fileContent.Length,
                Hash = hash,
                LfsPath = filePath,
                Description = description ?? string.Empty,
                CreatedBy = uploadedBy,
                CreatedAt = DateTime.UtcNow,
                LastUpdatedAt = DateTime.UtcNow,
                LastUpdatedBy = uploadedBy
            };

            _db.CaseFiles.Add(fileMetadata);

            // update the parent case LastUpdatedAt timestamp
            var caseEntity = await _db.Cases.FindAsync(caseId);
            if (caseEntity != null)
            {
                caseEntity.LastUpdatedAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();

            _logger.LogInformation("Saved case file {FileId} ({Size} bytes) to case {CaseId}",
                fileId, fileContent.Length, caseId);

            var uri = $"auxlfs://localhost/files/{fileId}?size={fileContent.Length}&hash={hash}";
            return (uri, fileMetadata);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save case file");
            throw;
        }
    }

    public async Task DeleteCaseFileAsync(Guid fileId)
    {
        try
        {
            var metadata = await GetCaseFileMetadataAsync(fileId)
                ?? throw new KeyNotFoundException($"Case file {fileId} not found");

            _db.CaseFiles.Remove(metadata);

            // update the parent case LastUpdatedAt timestamp
            var caseEntity = await _db.Cases.FindAsync(metadata.CaseId);
            if (caseEntity != null)
            {
                caseEntity.LastUpdatedAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();

            // delete the actual file
            var filePath = Path.Combine(_lfsBasePath, $"{fileId}.bin");
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _logger.LogInformation("Deleted file from LFS: {FilePath}", filePath);
            }

            _logger.LogInformation("Deleted case file {FileId}", fileId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete case file {FileId}", fileId);
            throw;
        }
    }
    #endregion

    #region user file operations
    public async Task<UserFileModel?> GetUserFileMetadataAsync(Guid fileId)
    {
        try
        {
            return await _db.UserFiles
                .Include(f => f.User)
                .Include(f => f.CreatedByUser)
                .FirstOrDefaultAsync(f => f.Id == fileId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user file metadata for {FileId}", fileId);
            return null;
        }
    }

    public async Task<List<UserFileModel>> GetFilesForUserAsync(Guid userId)
    {
        try
        {
            return await _db.UserFiles
                .Include(f => f.CreatedByUser)
                .Where(f => f.UserId == userId)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get files for user {UserId}", userId);
            throw;
        }
    }

    public async Task<(string uri, UserFileModel metadata)> SaveUserFileAsync(
        byte[] fileContent,
        string filename,
        string contentType,
        Guid uploadedBy,
        Guid userId,
        string? description = null)
    {
        try
        {
            var fileId = UUIDUtilities.GenerateV5(DatabaseObjectType.File);
            var filePath = Path.Combine(_lfsBasePath, $"{fileId}.bin");

            // just make sure the directory actually exists
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // write the file to lfs
            await File.WriteAllBytesAsync(filePath, fileContent);
            var hash = HashingUtilities.SHA256Hash(fileContent);

            // create metadata
            var fileMetadata = new UserFileModel
            {
                Id = fileId,
                UserId = userId,
                Filename = filename,
                ContentType = contentType,
                Size = fileContent.Length,
                Hash = hash,
                LfsPath = filePath,
                Description = description ?? string.Empty,
                CreatedBy = uploadedBy,
                CreatedAt = DateTime.UtcNow,
                LastUpdatedAt = DateTime.UtcNow,
                LastUpdatedBy = uploadedBy
            };

            _db.UserFiles.Add(fileMetadata);

            // update the parent user's LastUpdatedAt timestamp
            var userEntity = await _db.Users.FindAsync(userId);
            if (userEntity != null)
            {
                userEntity.LastUpdatedAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();

            _logger.LogInformation("Saved user file {FileId} ({Size} bytes) to user {UserId}",
                fileId, fileContent.Length, userId);

            var uri = $"auxlfs://localhost/files/{fileId}?size={fileContent.Length}&hash={hash}";
            return (uri, fileMetadata);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save user file");
            throw;
        }
    }

    public async Task DeleteUserFileAsync(Guid fileId)
    {
        try
        {
            var metadata = await GetUserFileMetadataAsync(fileId)
                ?? throw new KeyNotFoundException($"User file {fileId} not found");

            _db.UserFiles.Remove(metadata);

            // update the LastUpdatedAt timestamp
            var userEntity = await _db.Users.FindAsync(metadata.UserId);
            if (userEntity != null)
            {
                userEntity.LastUpdatedAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();

            // delete the file itself
            var filePath = Path.Combine(_lfsBasePath, $"{fileId}.bin");
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _logger.LogInformation("Deleted file from LFS: {FilePath}", filePath);
            }

            _logger.LogInformation("Deleted user file {FileId}", fileId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete user file {FileId}", fileId);
            throw;
        }
    }
    #endregion

    #region shared file operations
    public async Task<byte[]?> GetFileContentsAsync(Guid fileId)
    {
        try
        {
            var filePath = Path.Combine(_lfsBasePath, $"{fileId}.bin");

            if (!File.Exists(filePath))
            {
                _logger.LogWarning("File not found in LFS: {FilePath}", filePath);
                return null;
            }

            return await File.ReadAllBytesAsync(filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get file contents for {FileId}", fileId);
            return null;
        }
    }

    public async Task<bool> CheckCaseFileAccessAsync(Guid fileId, UserModel currentUser)
    {
        try
        {
            var file = await _db.CaseFiles
                .Include(f => f.Case)
                    .ThenInclude(c => c.Workers)
                .Include(f => f.Case)
                    .ThenInclude(c => c.Clients)
                .FirstOrDefaultAsync(f => f.Id == fileId);

            if (file == null) return false;
            if (currentUser.IsAdmin) return true;

            return file.Case.Workers.Any(w => w.UserId == currentUser.Id) ||
                   file.Case.Clients.Any(c => c.UserId == currentUser.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check access for case file {FileId}", fileId);
            return false;
        }
    }

    public async Task<bool> CheckUserFileAccessAsync(Guid fileId, UserModel currentUser)
    {
        try
        {
            var file = await _db.UserFiles.FirstOrDefaultAsync(f => f.Id == fileId);
            if (file == null) return false;

            // only admins or file owner
            return currentUser.IsAdmin || file.UserId == currentUser.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check access for user file {FileId}", fileId);
            return false;
        }
    }
    #endregion
}
