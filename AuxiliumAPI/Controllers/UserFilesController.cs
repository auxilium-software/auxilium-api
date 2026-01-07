using AuxiliumAPI.Common.ControllerBases;
using AuxiliumAPI.Common.EF;
using AuxiliumAPI.Common.Services.Interfaces;
using AuxiliumAPI.Models;
using AuxiliumAPI.Models.File;
using Microsoft.AspNetCore.Mvc;

namespace AuxiliumAPI.Controllers;

[ApiController]
[Route("/api/v3/users/{userId}/files")]
[Tags("Users", "Files")]
public class UserFilesController : LoggedInControllerBase
{
    private readonly IFileDocumentService _fileService;
    private readonly IUserDocumentService _userDocService;
    private readonly ILogger<UserFilesController> _logger;

    public UserFilesController(
        IFileDocumentService fileService,
        IUserDocumentService userDocService,
        AuxiliumDbContext db,
        ILogger<UserFilesController> logger)
        : base(db, logger)
    {
        _fileService = fileService;
        _userDocService = userDocService;
        _logger = logger;
    }

    [HttpPost("")]
    [ProducesResponseType(typeof(FileDetailsResponseModel), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<FileDetailsResponseModel>> UploadFile(
        string userId,
        [FromForm] FileUploadRequestModel request)
    {
        try
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            if (!Guid.TryParse(userId, out var userGuid))
            {
                return BadRequest(new FailureResponseModel { Detail = "Invalid user ID" });
            }

            if (request.File == null || request.File.Length == 0)
            {
                return BadRequest(new FailureResponseModel { Detail = "No file provided" });
            }

            // check user access
            if (!_userDocService.CheckUserAccess(userGuid, user!))
            {
                return StatusCode(403, new FailureResponseModel
                {
                    Detail = "You don't have permission to upload files to this user"
                });
            }

            // read file contents
            using var memoryStream = new MemoryStream();
            await request.File.CopyToAsync(memoryStream);
            var fileBytes = memoryStream.ToArray();

            // save the file
            var (uri, fileMetadata) = await _fileService.SaveUserFileAsync(
                fileContent: fileBytes,
                filename: request.File.FileName,
                contentType: request.File.ContentType ?? "application/octet-stream",
                uploadedBy: user!.Id,
                userId: userGuid,
                description: request.Description
            );

            _logger.LogInformation(
                "Uploaded file {FileId} ({Size} bytes) to user {UserId}",
                fileMetadata.Id, fileBytes.Length, userId
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
            _logger.LogError(ex, "Failed to upload file to user {CaseId}", userId);
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
                return BadRequest(new FailureResponseModel { Detail = "Invalid user ID" });
            }

            if (!_userDocService.CheckUserAccess(caseGuid, user!))
            {
                return StatusCode(403, new FailureResponseModel
                {
                    Detail = "You don't have permission to access this user"
                });
            }

            var fileMetadata = await _fileService.GetUserFileMetadataAsync(fileId);
            if (fileMetadata == null)
            {
                return NotFound(new FailureResponseModel { Detail = "File not found" });
            }

            // verify the file belongs to this user
            if (fileMetadata.UserId != caseGuid)
            {
                return NotFound(new FailureResponseModel { Detail = "File not found in this user" });
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
            _logger.LogError(ex, "Failed to get file {FileId} from user {CaseId}", fileId, caseId);
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
                return BadRequest(new FailureResponseModel { Detail = "Invalid user ID" });
            }

            if (!_userDocService.CheckUserAccess(caseGuid, user!))
            {
                return StatusCode(403, new FailureResponseModel
                {
                    Detail = "You don't have permission to access this user"
                });
            }

            var fileMetadata = await _fileService.GetUserFileMetadataAsync(fileId);
            if (fileMetadata == null)
            {
                return NotFound(new FailureResponseModel { Detail = "File not found" });
            }

            if (fileMetadata.UserId != caseGuid)
            {
                return NotFound(new FailureResponseModel { Detail = "File not found in this user" });
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
            _logger.LogError(ex, "Failed to render file {FileId} from user {CaseId}", fileId, caseId);
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
                return BadRequest(new FailureResponseModel { Detail = "Invalid user ID" });
            }

            var caseDoc = await _userDocService.GetDocumentAsync(caseGuid);
            if (caseDoc == null)
            {
                return NotFound(new FailureResponseModel { Detail = "User not found" });
            }

            // only admins can delete files
            if (!user!.IsAdmin)
            {
                return StatusCode(403, new FailureResponseModel
                {
                    Detail = "Only user workers and admins can delete files"
                });
            }

            var fileMetadata = await _fileService.GetUserFileMetadataAsync(fileId);
            if (fileMetadata == null)
            {
                return NotFound(new FailureResponseModel { Detail = "File not found" });
            }

            if (fileMetadata.UserId != caseGuid)
            {
                return NotFound(new FailureResponseModel { Detail = "File not found in this user" });
            }

            await _fileService.DeleteUserFileAsync(fileId);

            _logger.LogInformation(
                "Deleted file {FileId} from user {CaseId} by user {UserId}",
                fileId, caseId, user.Id
            );

            return Ok(new SuccessResponseModel());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete file {FileId} from user {CaseId}", fileId, caseId);
            return StatusCode(500, new FailureResponseModel { Detail = "Failed to delete file" });
        }
    }
}
