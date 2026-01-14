using AuxiliumSoftware.AuxiliumServices.Common.EF;
using AuxiliumSoftware.AuxiliumServices.Common.Services.Interfaces;
using AuxiliumAPI.Models;
using Microsoft.AspNetCore.Mvc;
using AuxiliumSoftware.AuxiliumServices.API.Models;
using AuxiliumSoftware.AuxiliumServices.API.Common.ControllerBases;
using AuxiliumSoftware.AuxiliumServices.API.Models.File;

namespace AuxiliumSoftware.AuxiliumServices.API.Controllers;

[ApiController]
[Route("/api/v3/cases/{caseId}/files")]
[Tags("Cases", "Files")]
public class CaseFilesController : LoggedInControllerBase
{
    private readonly ILogger<CaseFilesController> _logger;

    private readonly IFileDocumentService _fileService;
    private readonly ICaseDocumentService _caseDocService;

    public CaseFilesController(
        ILogger<CaseFilesController> logger,
        IFileDocumentService fileService,
        ICaseDocumentService caseDocService,
        AuxiliumDbContext db
        )
        : base(db, logger)
    {
        _logger = logger;
        _fileService = fileService;
        _caseDocService = caseDocService;
    }

    [HttpPost("")]
    [ProducesResponseType(typeof(FileDetailsResponseModel), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<FileDetailsResponseModel>> UploadFile(
        string caseId,
        [FromForm] FileUploadRequestModel request)
    {
        try
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            if (!Guid.TryParse(caseId, out var caseGuid))
            {
                return BadRequest(new FailureResponseModel { Detail = "Invalid case ID" });
            }

            if (request.File == null || request.File.Length == 0)
            {
                return BadRequest(new FailureResponseModel { Detail = "No file provided" });
            }

            // check case access
            if (!await _caseDocService.CheckUserAccessAsync(caseGuid, user!))
            {
                return StatusCode(403, new FailureResponseModel
                {
                    Detail = "You don't have permission to upload files to this case"
                });
            }

            // read the contents of the file
            using var memoryStream = new MemoryStream();
            await request.File.CopyToAsync(memoryStream);
            var fileBytes = memoryStream.ToArray();

            // save the file
            var (uri, fileMetadata) = await _fileService.SaveCaseFileAsync(
                fileContent: fileBytes,
                filename: request.File.FileName,
                contentType: request.File.ContentType ?? "application/octet-stream",
                uploadedBy: user!.Id,
                caseId: caseGuid,
                description: request.Description
            );

            _logger.LogInformation(
                "Uploaded file {FileId} ({Size} bytes) to case {CaseId}",
                fileMetadata.Id, fileBytes.Length, caseId
            );

            return StatusCode(201, new FileDetailsResponseModel
            {
                Id = fileMetadata.Id,
                Filename = fileMetadata.Filename,
                ContentType = fileMetadata.ContentType,
                Hash = fileMetadata.Hash,
                Size = fileMetadata.Size,
                CreatedAt = fileMetadata.CreatedAt,
                CreatedBy = fileMetadata.CreatedBy,
                Description = fileMetadata.Description ?? string.Empty
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload file to case {CaseId}", caseId);
            return StatusCode(500, new FailureResponseModel { Detail = "Failed to upload file" });
        }
    }

    [HttpGet("{fileId:guid}")]
    [ProducesResponseType(typeof(FileDetailsResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<FileDetailsResponseModel>> GetFile(string caseId, Guid fileId)
    {
        try
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            if (!Guid.TryParse(caseId, out var caseGuid))
            {
                return BadRequest(new FailureResponseModel { Detail = "Invalid case ID" });
            }

            if (!await _caseDocService.CheckUserAccessAsync(caseGuid, user!))
            {
                return StatusCode(403, new FailureResponseModel
                {
                    Detail = "You don't have permission to access this case"
                });
            }

            var fileMetadata = await _fileService.GetCaseFileMetadataAsync(fileId);
            if (fileMetadata == null)
            {
                return NotFound(new FailureResponseModel { Detail = "File not found" });
            }

            // verify file belongs to this case
            if (fileMetadata.CaseId != caseGuid)
            {
                return NotFound(new FailureResponseModel { Detail = "File not found in this case" });
            }

            return Ok(new FileDetailsResponseModel
            {
                Id = fileMetadata.Id,
                Filename = fileMetadata.Filename,
                ContentType = fileMetadata.ContentType,
                Hash = fileMetadata.Hash,
                Size = fileMetadata.Size,
                CreatedAt = fileMetadata.CreatedAt,
                CreatedBy = fileMetadata.CreatedBy,
                Description = fileMetadata.Description ?? string.Empty
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get file {FileId} from case {CaseId}", fileId, caseId);
            return StatusCode(500, new FailureResponseModel { Detail = "Failed to get file" });
        }
    }

    [HttpGet("{fileId:guid}/render")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RenderFile(string caseId, Guid fileId)
    {
        try
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            if (!Guid.TryParse(caseId, out var caseGuid))
            {
                return BadRequest(new FailureResponseModel { Detail = "Invalid case ID" });
            }

            if (!await _caseDocService.CheckUserAccessAsync(caseGuid, user!))
            {
                return StatusCode(403, new FailureResponseModel
                {
                    Detail = "You don't have permission to access this case"
                });
            }

            var fileMetadata = await _fileService.GetCaseFileMetadataAsync(fileId);
            if (fileMetadata == null)
            {
                return NotFound(new FailureResponseModel { Detail = "File not found" });
            }

            if (fileMetadata.CaseId != caseGuid)
            {
                return NotFound(new FailureResponseModel { Detail = "File not found in this case" });
            }

            var fileBytes = await _fileService.GetFileContentsAsync(fileId);
            if (fileBytes == null || fileBytes.Length == 0)
            {
                return NotFound(new FailureResponseModel { Detail = "File content not found" });
            }

            Response.Headers.Append("Accept-Ranges", "bytes");
            Response.Headers.Append("Content-Length", fileBytes.Length.ToString());
            Response.Headers.Append("Cache-Control", "public, max-age=3600");
            Response.Headers.Append("Content-Disposition", $"inline; filename=\"{fileMetadata.Filename}\"");

            return File(fileBytes, fileMetadata.ContentType ?? "application/octet-stream");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to render file {FileId} from case {CaseId}", fileId, caseId);
            return StatusCode(500, new FailureResponseModel { Detail = "Failed to render file" });
        }
    }

    [HttpDelete("{fileId:guid}")]
    [ProducesResponseType(typeof(SuccessResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SuccessResponseModel>> DeleteFile(string caseId, Guid fileId)
    {
        try
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            if (!Guid.TryParse(caseId, out var caseGuid))
            {
                return BadRequest(new FailureResponseModel { Detail = "Invalid case ID" });
            }

            var caseDoc = await _caseDocService.GetDocumentAsync(caseGuid);
            if (caseDoc == null)
            {
                return NotFound(new FailureResponseModel { Detail = "Case not found" });
            }

            // only case workers and admins can delete files
            var isWorker = caseDoc.Workers?.Any(w => w.UserId == user!.Id) ?? false;
            if (!user!.IsAdmin && !isWorker)
            {
                return StatusCode(403, new FailureResponseModel
                {
                    Detail = "Only case workers and admins can delete files"
                });
            }

            var fileMetadata = await _fileService.GetCaseFileMetadataAsync(fileId);
            if (fileMetadata == null)
            {
                return NotFound(new FailureResponseModel { Detail = "File not found" });
            }

            if (fileMetadata.CaseId != caseGuid)
            {
                return NotFound(new FailureResponseModel { Detail = "File not found in this case" });
            }

            await _fileService.DeleteCaseFileAsync(fileId);

            _logger.LogInformation(
                "Deleted file {FileId} from case {CaseId} by user {UserId}",
                fileId, caseId, user.Id
            );

            return Ok(new SuccessResponseModel());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete file {FileId} from case {CaseId}", fileId, caseId);
            return StatusCode(500, new FailureResponseModel { Detail = "Failed to delete file" });
        }
    }
}
