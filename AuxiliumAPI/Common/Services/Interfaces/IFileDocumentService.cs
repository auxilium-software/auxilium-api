using AuxiliumAPI.Common.EntityModels;

namespace AuxiliumAPI.Common.Services.Interfaces;

public interface IFileDocumentService
{
    Task<CaseFileModel?> GetCaseFileMetadataAsync(Guid fileId);
    Task<List<CaseFileModel>> GetFilesForCaseAsync(Guid caseId);
    Task<(string uri, CaseFileModel metadata)> SaveCaseFileAsync(
        byte[] fileContent,
        string filename,
        string contentType,
        Guid uploadedBy,
        Guid caseId,
        string? description = null);
    Task DeleteCaseFileAsync(Guid fileId);
    Task<bool> CheckCaseFileAccessAsync(Guid fileId, UserModel currentUser);



    Task<UserFileModel?> GetUserFileMetadataAsync(Guid fileId);
    Task<List<UserFileModel>> GetFilesForUserAsync(Guid userId);
    Task<(string uri, UserFileModel metadata)> SaveUserFileAsync(
        byte[] fileContent,
        string filename,
        string contentType,
        Guid uploadedBy,
        Guid userId,
        string? description = null);
    Task DeleteUserFileAsync(Guid fileId);
    Task<bool> CheckUserFileAccessAsync(Guid fileId, UserModel currentUser);



    Task<byte[]?> GetFileContentsAsync(Guid fileId);
}
