using AuxiliumAPI.Common.ControllerBases;
using AuxiliumAPI.Common.Services.Interfaces;
using AuxiliumAPI.Models;
using AuxiliumAPI.Models.File;
using Microsoft.AspNetCore.Mvc;

namespace AuxiliumAPI.Controllers;

[ApiController]
[Route("api/v3/cases/{caseId}/files")]
[Tags("Cases", "Files")]
public class CaseFilesController : LoggedInControllerBase
{
    private readonly IFileService _fileService;
    private readonly ICaseDocumentService _caseDocService;
    private readonly ILogger<CaseFilesController> _logger;

    public CaseFilesController(
        IFileService fileService,
        ICaseDocumentService caseDocService,
        ILogger<CaseFilesController> logger,
        IMariaDbService mariaDb)
        : base(mariaDb, logger)
    {
        _fileService = fileService;
        _caseDocService = caseDocService;
        _logger = logger;
    }

    [HttpPost("")]
    [ProducesResponseType(typeof(FileDetailsResponseModel), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<FileDetailsResponseModel>> UploadFile(
        string caseId,
        [FromForm] FileUploadRequestModel request
        )
    {
        try
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            // checks the given case id is a valid uuid
            if (!Guid.TryParse(caseId, out _))
            {
                return BadRequest(new FailureResponseModel { Detail = "Invalid case ID" });
            }

            // check there is a file
            if (request.File == null || request.File.Length == 0)
            {
                return BadRequest(new FailureResponseModel { Detail = "No file provided" });
            }

            // checks the case exists and user has access
            if (!await _caseDocService.CheckUserAccessAsync(Guid.Parse(caseId), user!))
            {
                return StatusCode(403, new FailureResponseModel
                {
                    Detail = "You don't have permission to upload files to this case"
                });
            }

            // grab the uploaded file contents
            using var memoryStream = new MemoryStream();
            await request.File.CopyToAsync(memoryStream);
            var fileBytes = memoryStream.ToArray();

            // save file
            var (auxLFSURL, fileMetadata) = await _fileService.SaveFileAsync(
                fileContent: fileBytes,
                filename: request.File.FileName,
                contentType: request.File.ContentType ?? "application/octet-stream",
                uploadedBy: user.id,
                parentType: FileParentTypeEnum.Case,
                parentId: Guid.Parse(caseId),
                description: request.Description
            );

            // add a reference to the file to the case doc
            var caseDoc = await _caseDocService.GetDocumentAsync(Guid.Parse(caseId));
            if (caseDoc != null)
            {
                caseDoc.Files ??= [];
                caseDoc.Files.Add(auxLFSURL);
                await _caseDocService.SaveDocumentAsync(caseDoc);
            }

            // return
            return StatusCode(201, new FileDetailsResponseModel
            {
                Id = Guid.Parse(fileMetadata.Id),
                Filename = fileMetadata.Filename,
                ContentType = fileMetadata.ContentType,
                Hash = fileMetadata.Hash,
                Size = fileMetadata.Size,
                CreatedAt = fileMetadata.CreatedAt,
                CreatedBy = fileMetadata.CreatedBy,
                Description = fileMetadata.Description
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload file to case {CaseId}", caseId);
            return StatusCode(500, new FailureResponseModel { Detail = "Failed to upload file" });
        }
    }

    [HttpGet("{fileId}")]
    [ProducesResponseType(typeof(FileDetailsResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<FileDetailsResponseModel>> GetFile(string caseId, string fileId)
    {
        try
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            // ensures the given case id is a valid id
            if (!Guid.TryParse(caseId, out _) || !Guid.TryParse(fileId, out _))
            {
                return BadRequest(new FailureResponseModel { Detail = "Invalid ID" });
            }

            // check whether the user has access to the case
            if (!await _caseDocService.CheckUserAccessAsync(Guid.Parse(caseId), user!))
            {
                return StatusCode(403, new FailureResponseModel
                {
                    Detail = "You don't have permission to access this case"
                });
            }

            var fileMetadata = await _fileService.GetFileMetadataAsync(Guid.Parse(fileId));
            if (fileMetadata == null)
            {
                return NotFound(new FailureResponseModel { Detail = "File not found" });
            }

            // Verify file belongs to this case
            if (fileMetadata.ParentId != Guid.Parse(caseId))
            {
                return NotFound(new FailureResponseModel { Detail = "File not found in this case" });
            }

            return Ok(new FileDetailsResponseModel
            {
                Id = Guid.Parse(fileMetadata.Id),
                Filename = fileMetadata.Filename,
                ContentType = fileMetadata.ContentType,
                Hash = fileMetadata.Hash,
                Size = fileMetadata.Size,
                CreatedAt = fileMetadata.CreatedAt,
                CreatedBy = fileMetadata.CreatedBy,
                Description = fileMetadata.Description
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get file {FileId} from case {CaseId}", fileId, caseId);
            return StatusCode(500, new FailureResponseModel { Detail = "Failed to get file" });
        }
    }

    [HttpGet("{fileId}/render")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RenderFile(string caseId, string fileId)
    {
        try
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            // ensures that the given case id is a valid uuid
            if (!Guid.TryParse(caseId, out _) || !Guid.TryParse(fileId, out _))
            {
                return BadRequest(new FailureResponseModel { Detail = "Invalid ID" });
            }

            // check that the user has access to the case
            if (!await _caseDocService.CheckUserAccessAsync(Guid.Parse(caseId), user!))
            {
                return StatusCode(403, new FailureResponseModel
                {
                    Detail = "You don't have permission to access this case"
                });
            }

            // grab the metadata for the file
            var fileMetadata = await _fileService.GetFileMetadataAsync(Guid.Parse(fileId));
            if (fileMetadata == null)
            {
                return NotFound(new FailureResponseModel { Detail = "File not found" });
            }

            // just check that the file actually belongs to this case
            if (fileMetadata.ParentId != Guid.Parse(caseId))
            {
                return NotFound(new FailureResponseModel { Detail = "File not found in this case" });
            }

            // grab the file contents from lfs
            var fileBytes = await _fileService.GetFileContentsAsync(Guid.Parse(fileId));
            if (fileBytes == null || fileBytes.Length == 0)
            {
                return NotFound(new FailureResponseModel { Detail = "File content not found" });
            }

            // return file with the additional headers
            Response.Headers.Append("Accept-Ranges", "bytes");
            Response.Headers.Append("Content-Length", fileBytes.Length.ToString());
            Response.Headers.Append("Cache-Control", "public, max-age=3600");
            Response.Headers.Append("Content-Disposition", $"inline; filename=\"{fileMetadata.Filename}\"");

            // return
            return File(fileBytes, fileMetadata.ContentType ?? "application/octet-stream");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to render file {FileId} from case {CaseId}", fileId, caseId);
            return StatusCode(500, new FailureResponseModel { Detail = "Failed to render file" });
        }
    }

    [HttpDelete("{fileId}")]
    [ProducesResponseType(typeof(SuccessResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SuccessResponseModel>> DeleteFile(string caseId, string fileId)
    {
        try
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            // ensures the given case id is a valid uuid
            if (!Guid.TryParse(caseId, out _) || !Guid.TryParse(fileId, out _))
            {
                return BadRequest(new FailureResponseModel { Detail = "Invalid ID" });
            }

            // check the user has access to this case
            var caseDoc = await _caseDocService.GetDocumentAsync(Guid.Parse(caseId));
            if (caseDoc == null)
            {
                return NotFound(new FailureResponseModel { Detail = "Case not found" });
            }

            // ensures that the user is either an admin or a case worker
            if (!user!.is_admin && !caseDoc.Workers.Contains(user.id))
            {
                return StatusCode(403, new FailureResponseModel
                {
                    Detail = "Only case workers and admins can delete files"
                });
            }

            // grab the file metadata
            var fileMetadata = await _fileService.GetFileMetadataAsync(Guid.Parse(fileId));
            if (fileMetadata == null)
            {
                return NotFound(new FailureResponseModel { Detail = "File not found" });
            }

            // check that this file actually belongs to this case
            if (fileMetadata.ParentId != Guid.Parse(caseId))
            {
                return NotFound(new FailureResponseModel { Detail = "File not found in this case" });
            }

            // delete the file (this method will also remove it from the case doc)
            await _fileService.DeleteFileAsync(Guid.Parse(fileId));

            // return
            return Ok(new SuccessResponseModel());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete file {FileId} from case {CaseId}", fileId, caseId);
            return StatusCode(500, new FailureResponseModel { Detail = "Failed to delete file" });
        }
    }
}
