using AuxiliumSoftware.AuxiliumServices.API.Common.ControllerBases;
using AuxiliumSoftware.AuxiliumServices.API.Common.Utilities;
using AuxiliumSoftware.AuxiliumServices.API.Models;
using AuxiliumSoftware.AuxiliumServices.API.Models.File;
using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework;
using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework.EntityModels;
using AuxiliumSoftware.AuxiliumServices.Common.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuxiliumSoftware.AuxiliumServices.API.Controllers;

[ApiController]
[Route("/api/v3/users/{userId:guid}/files")]
[Tags("Users", "Files")]
public class SingleUserFilesController : LoggedInControllerBase
{
    private readonly IFileDocumentService _fileService;

    public SingleUserFilesController(
        ISystemSettingsService systemSettingsService,
        IConfiguration configuration,
        AuxiliumDbContext db,
        IWebApplicationFirewallService waf,
        ILogger<SingleUserFilesController> logger,
        ITotpService totpService,
        IFileDocumentService fileService
    )
        : base(systemSettingsService, configuration, db, waf, logger, totpService)
    {
        _fileService = fileService;
    }

    [HttpPost("")]
    [ProducesResponseType(typeof(FileDetailsResponseModel), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<FileDetailsResponseModel>> UploadFile(
        Guid userId,
        [FromForm] FileUploadRequestModel request)
    {
        try
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            var targetUser = await Db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (targetUser == null)
            {
                return NotFound(new FailureResponseModel { Detail = "User not found" });
            }

            if (!await CanUploadToUser(user!, userId))
            {
                return StatusCode(403, new FailureResponseModel
                {
                    Detail = "You don't have permission to upload files to this user"
                });
            }

            if (request.File == null || request.File.Length == 0)
            {
                return BadRequest(new FailureResponseModel { Detail = "No file provided" });
            }

            using var memoryStream = new MemoryStream();
            await request.File.CopyToAsync(memoryStream);
            var fileBytes = memoryStream.ToArray();

            var (uri, fileMetadata) = await _fileService.SaveUserFileAsync(
                fileContent: fileBytes,
                filename: request.File.FileName,
                contentType: request.File.ContentType ?? "application/octet-stream",
                uploadedBy: user!.Id,
                userId: userId,
                description: request.Description
            );

            Logger.LogInformation(
                "Uploaded file {FileId} ({Size} bytes) to user {UserId} by {UploadedBy}",
                fileMetadata.Id, fileBytes.Length, userId, user.Id
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
            Logger.LogError(ex, "Failed to upload file to user {UserId}", userId);
            return StatusCode(500, new FailureResponseModel { Detail = "Failed to upload file" });
        }
    }

    [HttpGet("{fileId:guid}")]
    [ProducesResponseType(typeof(FileDetailsResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<FileDetailsResponseModel>> GetFile(Guid userId, Guid fileId)
    {
        try
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            if (!await _fileService.CheckUserFileAccessAsync(fileId, user!))
            {
                return StatusCode(403, new FailureResponseModel
                {
                    Detail = "You don't have permission to access this file"
                });
            }

            var fileMetadata = await _fileService.GetUserFileMetadataAsync(fileId);
            if (fileMetadata == null)
            {
                return NotFound(new FailureResponseModel { Detail = "File not found" });
            }

            if (fileMetadata.UserId != userId)
            {
                return NotFound(new FailureResponseModel { Detail = "File not found for this user" });
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
            Logger.LogError(ex, "Failed to get file {FileId} for user {UserId}", fileId, userId);
            return StatusCode(500, new FailureResponseModel { Detail = "Failed to get file" });
        }
    }

    [HttpGet("{fileId:guid}/render")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RenderFile(Guid userId, Guid fileId)
    {
        try
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            if (!await _fileService.CheckUserFileAccessAsync(fileId, user!))
            {
                return StatusCode(403, new FailureResponseModel
                {
                    Detail = "You don't have permission to access this file"
                });
            }

            var fileMetadata = await _fileService.GetUserFileMetadataAsync(fileId);
            if (fileMetadata == null)
            {
                return NotFound(new FailureResponseModel { Detail = "File not found" });
            }

            if (fileMetadata.UserId != userId)
            {
                return NotFound(new FailureResponseModel { Detail = "File not found for this user" });
            }

            var fileBytes = await _fileService.GetFileContentsAsync(fileId);
            if (fileBytes == null || fileBytes.Length == 0)
            {
                return NotFound(new FailureResponseModel { Detail = "File content not found" });
            }

            var contentType = fileMetadata.ContentType ?? "application/octet-stream";
            var disposition = FileUtilities.IsScriptCapable(contentType) ? "attachment" : "inline";
            var safeName = Path.GetFileName(fileMetadata.Filename).Replace("\"", "");

            Response.Headers.Append("Accept-Ranges", "bytes");
            Response.Headers.Append("Content-Length", fileBytes.Length.ToString());
            Response.Headers.Append("Cache-Control", "private, max-age=3600");
            Response.Headers.Append("X-Content-Type-Options", "nosniff");
            Response.Headers.Append(
                "Content-Security-Policy",
                "default-src 'none'; style-src 'unsafe-inline'; sandbox"
            );
            Response.Headers.Append("Content-Disposition",
                $"{disposition}; filename=\"{safeName}\"");

            return File(fileBytes, contentType);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to render file {FileId} for user {UserId}", fileId, userId);
            return StatusCode(500, new FailureResponseModel { Detail = "Failed to render file" });
        }
    }

    [HttpDelete("{fileId:guid}")]
    [ProducesResponseType(typeof(SuccessResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SuccessResponseModel>> DeleteFile(Guid userId, Guid fileId)
    {
        try
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            var fileMetadata = await _fileService.GetUserFileMetadataAsync(fileId);
            if (fileMetadata == null)
            {
                return NotFound(new FailureResponseModel { Detail = "File not found" });
            }

            if (fileMetadata.UserId != userId)
            {
                return NotFound(new FailureResponseModel { Detail = "File not found for this user" });
            }

            // only the file owner or admins can delete
            if (!user!.IsAdministrator && user.Id != fileMetadata.UserId)
            {
                return StatusCode(403, new FailureResponseModel
                {
                    Detail = "Only file owners and administrators can delete files"
                });
            }

            await _fileService.DeleteUserFileAsync(fileId);

            Logger.LogInformation(
                "Deleted file {FileId} from user {UserId} by {DeletedBy}",
                fileId, userId, user.Id
            );

            return Ok(new SuccessResponseModel());
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to delete file {FileId} from user {UserId}", fileId, userId);
            return StatusCode(500, new FailureResponseModel { Detail = "Failed to delete file" });
        }
    }

    /// <summary>
    /// Checks whether the current user can upload files to the target user.
    /// - Users can upload to themselves
    /// - Administrators can upload to anyone
    /// - Case workers can upload to clients on their cases
    /// - Case worker managers can upload to clients or workers on their cases
    /// </summary>
    private async Task<bool> CanUploadToUser(
        UserEntityModel currentUser, Guid targetUserId)
    {
        if (currentUser.Id == targetUserId)
            return true;

        if (currentUser.IsAdministrator)
            return true;

        if (currentUser.IsCaseWorker)
        {
            var hasAccess = await Db.Cases
                .Where(c => c.Workers!.Any(w => w.UserId == currentUser.Id))
                .AnyAsync(c => c.Clients!.Any(cl => cl.UserId == targetUserId));

            if (hasAccess) return true;
        }

        if (currentUser.IsCaseWorkerManager)
        {
            var hasAccess = await Db.Cases
                .Where(c => c.Workers!.Any(w => w.UserId == currentUser.Id))
                .AnyAsync(c =>
                    c.Clients!.Any(cl => cl.UserId == targetUserId) ||
                    c.Workers!.Any(w => w.UserId == targetUserId));

            if (hasAccess) return true;
        }

        return false;
    }
}
