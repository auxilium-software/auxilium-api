using AuxiliumAPI.Common.DataStructures.CouchDB;
using AuxiliumAPI.Models.File;

namespace AuxiliumAPI.Common.Services.Interfaces
{
    public interface IFileService
    {
        Task<FileDocumentStructure?> GetFileMetadataAsync(Guid fileId);
        Task<byte[]?> GetFileContentsAsync(Guid fileId);
        Task DeleteFileAsync(Guid fileId);
        Task SaveFileMetadataAsync(FileDocumentStructure fileMetadata);
        Task<Tuple<string, FileDocumentStructure>> SaveFileAsync(
            byte[] fileContent,
            string filename,
            string contentType,
            Guid uploadedBy,
            FileParentTypeEnum parentType,
            Guid parentId,
            string? description = null
            );
    }
}
