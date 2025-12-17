using AuxiliumAPI.Common.DataStructures.CouchDB;
using AuxiliumAPI.Common.Services.Interfaces;
using AuxiliumAPI.Models.File;

namespace AuxiliumAPI.Common.Services
{
    public class FileService : IFileService
    {
        private readonly ICouchDbService _couchDb;
        private readonly IConfiguration _configuration;
        private readonly ILogger<FileService> _logger;
        private readonly string _filesDatabaseName;
        private readonly string _lfsBasePath;

        public FileService(
            ICouchDbService couchDb,
            IConfiguration configuration,
            ILogger<FileService> logger
            )
        {
            _couchDb = couchDb;
            _configuration = configuration;
            _logger = logger;

            _filesDatabaseName = _configuration["Databases:CouchDB:Databases:Files"];
            _lfsBasePath = _configuration["FileSystem:RootStorageDirectories:AuxLFS"];
        }

        public async Task<FileDocumentStructure?> GetFileMetadataAsync(Guid fileId)
        {
            try
            {
                return await _couchDb.GetDocumentAsync<FileDocumentStructure>(
                    _filesDatabaseName,
                    fileId
                    );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get file metadata for {FileId}", fileId);
                return null;
            }
        }

        public async Task<byte[]?> GetFileContentsAsync(Guid fileId)
        {
            try
            {
                var metadata = await GetFileMetadataAsync(fileId);
                if (metadata == null)
                {
                    _logger.LogWarning("File metadata not found for {FileId}", fileId);
                    return null;
                }

                var filePath = Path.Combine(_lfsBasePath, metadata.Id.ToString() + ".bin");

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

        public async Task DeleteFileAsync(Guid fileId)
        {
            try
            {
                var metadata = await GetFileMetadataAsync(fileId);
                if (metadata == null)
                {
                    throw new KeyNotFoundException($"File {fileId} not found");
                }

                await RemoveFileFromParentAsync(metadata.ParentType, metadata.ParentId, fileId);

                var filePath = Path.Combine(_lfsBasePath, metadata.Id, ".bin");
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    _logger.LogInformation("Deleted file from LFS: {FilePath}", filePath);
                }

                await _couchDb.DeleteDocumentAsync(_filesDatabaseName, fileId);
                _logger.LogInformation("Deleted file metadata from CouchDB: {FileId}", fileId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete file {FileId}", fileId);
                throw;
            }
        }

        private async Task RemoveFileFromParentAsync(FileParentTypeEnum parentType, Guid parentId, Guid fileId)
        {
            try
            {
                switch (parentType)
                {
                    case FileParentTypeEnum.Case:
                        var casesDatabaseName = _configuration["Databases:CouchDB:Databases:Cases"]!;
                        var caseDoc = await _couchDb.GetDocumentAsync<CaseDocumentStructure>(
                            casesDatabaseName,
                            parentId
                        );

                        if (caseDoc != null)
                        {
                            caseDoc.Files.Remove(fileId.ToString());
                            caseDoc.LastUpdatedAt = DateTime.UtcNow;
                            await _couchDb.SaveDocumentAsync(casesDatabaseName, caseDoc);
                            _logger.LogInformation("Removed file {FileId} from case {CaseId}", fileId, parentId);
                        }
                        break;

                    case FileParentTypeEnum.User:
                        var usersDatabaseName = _configuration["Databases:CouchDB:Databases:Users"]!;
                        var userDoc = await _couchDb.GetDocumentAsync<UserDocumentStructure>(
                            usersDatabaseName,
                            parentId
                        );

                        if (userDoc != null)
                        {
                            userDoc.Files.Remove(fileId.ToString());
                            await _couchDb.SaveDocumentAsync(usersDatabaseName, userDoc);
                            _logger.LogInformation("Removed file {FileId} from user {UserId}", fileId, parentId);
                        }
                        break;

                    default:
                        _logger.LogWarning("Unknown parent type: {ParentType}", parentType);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to remove file from parent {ParentType}/{ParentId}", parentType, parentId);
                // don't throw - file deletion should still happen
            }
        }

        public async Task SaveFileMetadataAsync(FileDocumentStructure fileMetadata)
        {
            try
            {
                await _couchDb.SaveDocumentAsync(_filesDatabaseName, fileMetadata);
                _logger.LogInformation("Saved file metadata: {FileId}", fileMetadata.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save file metadata for {FileId}", fileMetadata.Id);
                throw;
            }
        }

        public async Task<Tuple<string, FileDocumentStructure>> SaveFileAsync(
            byte[] fileContent,
            string filename,
            string contentType,
            Guid uploadedBy,
            FileParentTypeEnum parentType,
            Guid parentId,
            string? description = null
            )
        {
            try
            {
                Guid fileId = Utilities.UUIDUtilities.GenerateV5(Enumerators.DatabaseObjectType.File);
                var filePath = Path.Combine(_lfsBasePath, fileId.ToString() + ".bin");

                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                await File.WriteAllBytesAsync(filePath, fileContent);

                var hash = Utilities.HashingUtilities.SHA256Hash(fileContent);

                var fileMetadata = new FileDocumentStructure
                {
                    Id = fileId.ToString(),
                    Filename = filename,
                    ContentType = contentType,
                    Hash = hash,
                    Size = fileContent.Length,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = uploadedBy,
                    ParentType = parentType,
                    ParentId = parentId,
                    Description = description
                };

                await SaveFileMetadataAsync(fileMetadata);

                return new(
                    $"auxlfs://localhost/files/{fileMetadata.Id}?size={fileContent.Length}&hash={hash}",
                    fileMetadata
                    );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save file");
                throw;
            }
        }
    }
}
