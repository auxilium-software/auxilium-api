using AuxiliumSoftware.AuxiliumServices.API.Common.ControllerBases;
using AuxiliumSoftware.AuxiliumServices.API.Common.Utilities;
using AuxiliumSoftware.AuxiliumServices.API.Models;
using AuxiliumSoftware.AuxiliumServices.API.Models.File;
using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework;
using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework.EntityModels;
using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework.Enumerators;
using AuxiliumSoftware.AuxiliumServices.Common.Services;
using Microsoft.AspNetCore.Mvc;

namespace AuxiliumSoftware.AuxiliumServices.API.Controllers;

[ApiController]
[Route("/api/v3/cases/{caseId:guid}/files")]
[Tags("Cases", "Files")]
public class SingleCaseFilesController : LoggedInControllerBase
{
    private readonly IFileDocumentService _fileService;
    private readonly ICaseDocumentService _caseDocService;

    public SingleCaseFilesController(
        ISystemSettingsService systemSettingsService,
        IConfiguration configuration,
        AuxiliumDbContext db,
        IWebApplicationFirewallService waf,
        ILogger<SingleCaseFilesController> logger,
        ITotpService totpService,

        IFileDocumentService fileService,
        ICaseDocumentService caseDocService
        )
        : base(systemSettingsService, configuration, db, waf, logger, totpService)
    {
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
        Guid caseId,
        [FromForm] FileUploadRequestModel request
    )
    {
        try
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            if (request.File == null || request.File.Length == 0)
            {
                return BadRequest(new FailureResponseModel { Detail = "No file provided" });
            }

            // check case access
            if (!await _caseDocService.CheckUserAccessAsync(caseId, user!))
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
                caseId: caseId,
                description: request.Description
            );

            this.Logger.LogInformation(
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
            this.Logger.LogError(ex, "Failed to upload file to case {CaseId}", caseId);
            return StatusCode(500, new FailureResponseModel { Detail = "Failed to upload file" });
        }
    }

    [HttpGet("{fileId:guid}")]
    [ProducesResponseType(typeof(FileDetailsResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<FileDetailsResponseModel>> GetFile(Guid caseId, Guid fileId)
    {
        try
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            if (!await _caseDocService.CheckUserAccessAsync(caseId, user!))
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
            if (fileMetadata.CaseId != caseId)
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
            this.Logger.LogError(ex, "Failed to get file {FileId} from case {CaseId}", fileId, caseId);
            return StatusCode(500, new FailureResponseModel { Detail = "Failed to get file" });
        }
    }

    [HttpGet("{fileId:guid}/render")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RenderFile(Guid caseId, Guid fileId)
    {
        try
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            if (!await _caseDocService.CheckUserAccessAsync(caseId, user!))
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

            if (fileMetadata.CaseId != caseId)
            {
                return NotFound(new FailureResponseModel { Detail = "File not found in this case" });
            }

            var fileBytes = await _fileService.GetFileContentsAsync(fileId);
            if (fileBytes == null || fileBytes.Length == 0)
            {
                return NotFound(new FailureResponseModel { Detail = "File content not found" });
            }

            var contentType = fileMetadata.ContentType ?? "application/octet-stream";
            var disposition = FileUtilities.IsScriptCapable(contentType) ? "attachment" : "inline";
            var safeName = Path.GetFileName(fileMetadata.Filename).Replace("\"", "");

            Response.Headers.Append("Accept-Ranges",            "bytes");
            Response.Headers.Append("Content-Length",           fileBytes.Length.ToString());
            Response.Headers.Append("Cache-Control",            "private, max-age=3600");
            Response.Headers.Append("X-Content-Type-Options",   "nosniff");
            Response.Headers.Append("Content-Security-Policy",  "default-src 'none'; style-src 'unsafe-inline'; sandbox");
            Response.Headers.Append("Content-Disposition",      $"{disposition}; filename=\"{safeName}\"");

            return File(fileBytes, contentType);
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Failed to render file {FileId} from case {CaseId}", fileId, caseId);
            return StatusCode(500, new FailureResponseModel { Detail = "Failed to render file" });
        }
    }

    [HttpDelete("{fileId:guid}")]
    [ProducesResponseType(typeof(SuccessResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SuccessResponseModel>> DeleteFile(Guid caseId, Guid fileId)
    {
        try
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            var caseDoc = await _caseDocService.GetDocumentAsync(caseId);
            if (caseDoc == null)
            {
                return NotFound(new FailureResponseModel { Detail = "Case not found" });
            }

            // only case workers and admins can delete files
            var isWorker = caseDoc.Workers?.Any(w => w.UserId == user!.Id) ?? false;
            if (!user!.IsAdministrator && !isWorker)
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

            if (fileMetadata.CaseId != caseId)
            {
                return NotFound(new FailureResponseModel { Detail = "File not found in this case" });
            }

            await _fileService.DeleteCaseFileAsync(fileId);

            this.Logger.LogInformation(
                "Deleted file {FileId} from case {CaseId} by user {UserId}",
                fileId, caseId, user.Id
            );

            return Ok(new SuccessResponseModel());
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Failed to delete file {FileId} from case {CaseId}", fileId, caseId);
            return StatusCode(500, new FailureResponseModel { Detail = "Failed to delete file" });
        }
    }
}
