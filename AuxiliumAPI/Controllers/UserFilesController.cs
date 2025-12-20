using AuxiliumAPI.Common.ControllerBases;
using AuxiliumAPI.Common.Services.Interfaces;
using AuxiliumAPI.Models;
using AuxiliumAPI.Models.File;
using Microsoft.AspNetCore.Mvc;

namespace AuxiliumAPI.Controllers
{
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
            ILogger<UserFilesController> logger,
            IMariaDbService mariaDb
            )
            : base(mariaDb, logger)
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
            [FromForm] FileUploadRequestModel request
            )
        {
            try
            {
                var (user, error) = await GetCurrentUserAsync();
                if (error != null) return error;

                // checks the given user id is a valid uuid
                if (!Guid.TryParse(userId, out _))
                {
                    return BadRequest(new FailureResponseModel { Detail = "Invalid user ID" });
                }

                // check there is a file
                if (request.File == null || request.File.Length == 0)
                {
                    return BadRequest(new FailureResponseModel { Detail = "No file provided" });
                }

                // checks the user exists and user has access
                if (!await _userDocService.CheckUserAccessAsync(Guid.Parse(userId), user!))
                {
                    return StatusCode(403, new FailureResponseModel
                    {
                        Detail = "You don't have permission to upload files to this user"
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
                    parentId: Guid.Parse(userId),
                    description: request.Description
                );

                // add a reference to the file to the case doc
                var userDoc = await _userDocService.GetDocumentAsync(Guid.Parse(userId));
                if (userDoc != null)
                {
                    userDoc.Files ??= [];
                    userDoc.Files.Add(auxLFSURL);
                    await _userDocService.SaveDocumentAsync(userDoc);
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
                _logger.LogError(ex, "Failed to upload file to user {UserId}", userId);
                return StatusCode(500, new FailureResponseModel { Detail = "Failed to upload file" });
            }
        }

        [HttpGet("{fileId}")]
        [ProducesResponseType(typeof(FileDetailsResponseModel), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<FileDetailsResponseModel>> GetFile(string userId, string fileId)
        {
            try
            {
                var (user, error) = await GetCurrentUserAsync();
                if (error != null) return error;

                // ensures the given user id is a valid id
                if (!Guid.TryParse(userId, out _) || !Guid.TryParse(fileId, out _))
                {
                    return BadRequest(new FailureResponseModel { Detail = "Invalid ID" });
                }

                // check whether the user has access to the user
                if (!await _userDocService.CheckUserAccessAsync(Guid.Parse(userId), user!))
                {
                    return StatusCode(403, new FailureResponseModel
                    {
                        Detail = "You don't have permission to access this user"
                    });
                }

                var fileMetadata = await _fileService.GetFileMetadataAsync(Guid.Parse(fileId));
                if (fileMetadata == null)
                {
                    return NotFound(new FailureResponseModel { Detail = "File not found" });
                }

                // Verify file belongs to this user
                if (fileMetadata.ParentId != Guid.Parse(userId))
                {
                    return NotFound(new FailureResponseModel { Detail = "File not found in this user" });
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
                _logger.LogError(ex, "Failed to get file {FileId} from user {UserId}", fileId, userId);
                return StatusCode(500, new FailureResponseModel { Detail = "Failed to get file" });
            }
        }

        [HttpGet("{fileId}/render")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> RenderFile(string userId, string fileId)
        {
            try
            {
                var (user, error) = await GetCurrentUserAsync();
                if (error != null) return error;

                // ensures that the given user id is a valid uuid
                if (!Guid.TryParse(userId, out _) || !Guid.TryParse(fileId, out _))
                {
                    return BadRequest(new FailureResponseModel { Detail = "Invalid ID" });
                }

                // check that the user has access to the user
                if (!await _userDocService.CheckUserAccessAsync(Guid.Parse(userId), user!))
                {
                    return StatusCode(403, new FailureResponseModel
                    {
                        Detail = "You don't have permission to access this user"
                    });
                }

                // grab the metadata for the file
                var fileMetadata = await _fileService.GetFileMetadataAsync(Guid.Parse(fileId));
                if (fileMetadata == null)
                {
                    return NotFound(new FailureResponseModel { Detail = "File not found" });
                }

                // just check that the file actually belongs to this user
                if (fileMetadata.ParentId != Guid.Parse(userId))
                {
                    return NotFound(new FailureResponseModel { Detail = "File not found in this user" });
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
                _logger.LogError(ex, "Failed to render file {FileId} from user {UserId}", fileId, userId);
                return StatusCode(500, new FailureResponseModel { Detail = "Failed to render file" });
            }
        }

        [HttpDelete("{fileId}")]
        [ProducesResponseType(typeof(SuccessResponseModel), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<SuccessResponseModel>> DeleteFile(string userId, string fileId)
        {
            try
            {
                var (user, error) = await GetCurrentUserAsync();
                if (error != null) return error;

                // ensures the given user id is a valid uuid
                if (!Guid.TryParse(userId, out _) || !Guid.TryParse(fileId, out _))
                {
                    return BadRequest(new FailureResponseModel { Detail = "Invalid ID" });
                }

                // check the user has access to this user
                var userDoc = await _userDocService.GetDocumentAsync(Guid.Parse(userId));
                if (userDoc == null)
                {
                    return NotFound(new FailureResponseModel { Detail = "User not found" });
                }

                // ensures that the user is either an admin or a user worker
                if (!user!.is_admin)
                {
                    return StatusCode(403, new FailureResponseModel
                    {
                        Detail = "Only user workers and admins can delete files"
                    });
                }

                // grab the file metadata
                var fileMetadata = await _fileService.GetFileMetadataAsync(Guid.Parse(fileId));
                if (fileMetadata == null)
                {
                    return NotFound(new FailureResponseModel { Detail = "File not found" });
                }

                // check that this file actually belongs to this user
                if (fileMetadata.ParentId != Guid.Parse(userId))
                {
                    return NotFound(new FailureResponseModel { Detail = "File not found in this user" });
                }

                // delete the file (this method will also remove it from the user doc)
                await _fileService.DeleteFileAsync(Guid.Parse(fileId));

                // return
                return Ok(new SuccessResponseModel());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete file {FileId} from user {UserId}", fileId, userId);
                return StatusCode(500, new FailureResponseModel { Detail = "Failed to delete file" });
            }
        }
    }
}
