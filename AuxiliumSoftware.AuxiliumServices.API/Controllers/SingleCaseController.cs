using AuxiliumSoftware.AuxiliumServices.API.Common.ControllerBases;
using AuxiliumSoftware.AuxiliumServices.API.Common.Utilities;
using AuxiliumSoftware.AuxiliumServices.API.Models;
using AuxiliumSoftware.AuxiliumServices.API.Models.Case;
using AuxiliumSoftware.AuxiliumServices.API.Models.File;
using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework;
using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework.Enumerators;
using AuxiliumSoftware.AuxiliumServices.Common.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuxiliumSoftware.AuxiliumServices.API.Controllers
{
    [ApiController]
    [Route("/api/v3/cases/{caseId:guid}")]
    [Tags("Cases")]
    [Authorize]
    public class SingleCaseController : LoggedInControllerBase
    {
        private readonly ICaseDocumentService _caseDocService;
        private readonly IFileDocumentService _fileService;

        public SingleCaseController(
            ISystemSettingsService systemSettingsService,
            IConfiguration configuration,
            AuxiliumDbContext db,
            IWebApplicationFirewallService waf,
            ILogger<SingleCaseController> logger,
            ITotpService totpService,

            ICaseDocumentService caseDocService,
            IFileDocumentService fileService
            )
            : base(systemSettingsService, configuration, db, waf, logger, totpService)
        {
            _caseDocService = caseDocService;
            _fileService = fileService;
        }








        [HttpGet("")]
        [ProducesResponseType(typeof(CaseResponseModel), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<CaseResponseModel>> GetCaseById(Guid caseId)
        {
            try
            {
                var (user, error) = await GetCurrentUserAsync();
                if (error != null) return error;

                var caseEntity = await Db.Cases
                    .Include(c => c.Clients)
                    .Include(c => c.Workers)
                    .Include(c => c.Files)
                    .Include(c => c.Messages)
                    .Include(c => c.Todos)
                    .Include(c => c.AdditionalProperties)
                    .FirstOrDefaultAsync(c => c.Id == caseId);

                if (caseEntity == null)
                {
                    return NotFound(new FailureResponseModel { Detail = "Case not found" });
                }

                // check access -> is admin OR client OR worker
                var hasAccess = user!.IsAdministrator ||
                              (caseEntity.Clients ?? []).Any(cl => cl.UserId == user.Id) ||
                              (caseEntity.Workers ?? []).Any(w => w.UserId == user.Id);

                if (!hasAccess)
                {
                    return StatusCode(403, new FailureResponseModel
                    {
                        Detail = "You don't have permission to view this case"
                    });
                }

                var response = ControllerUtilities.CaseMapToCaseResponseModel(caseEntity);
                return Ok(response);
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex, "Failed to fetch case {CaseId}", caseId);
                return StatusCode(500, new FailureResponseModel { Detail = "Failed to fetch case" });
            }
        }

        [HttpPost("upload")]
        [ProducesResponseType(typeof(SuccessResponseModel), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<SuccessResponseModel>> UploadFile(
            Guid caseId,
            [FromForm] FileUploadRequestModel request
        )
        {
            try
            {
                var (user, error) = await GetCurrentUserAsync();
                if (error != null) return error;

                // verify the cast actually exists
                var caseEntity = await Db.Cases
                    .Include(c => c.Workers)
                    .Include(c => c.Clients)
                    .FirstOrDefaultAsync(c => c.Id == caseId);

                if (caseEntity == null)
                {
                    return NotFound(new FailureResponseModel { Detail = "Case not found" });
                }

                // check the permissions - workers and clients can upload
                var canUpload = user!.IsAdministrator ||
                              (caseEntity.Workers ?? []).Any(w => w.UserId == user.Id) ||
                              (caseEntity.Clients ?? []).Any(c => c.UserId == user.Id);

                if (!canUpload)
                {
                    return StatusCode(403, new FailureResponseModel
                    {
                        Detail = "You don't have permission to upload files to this case"
                    });
                }

                // make sure there is actually a file
                if (request.File == null || request.File.Length == 0)
                {
                    return BadRequest(new FailureResponseModel { Detail = "No file provided" });
                }

                // read the file contents
                using var memoryStream = new MemoryStream();
                await request.File.CopyToAsync(memoryStream);
                var fileBytes = memoryStream.ToArray();

                var contentType = request.File.ContentType ?? "application/octet-stream";

                // save file using the file service
                var (uri, metadata) = await _fileService.SaveCaseFileAsync(
                    fileBytes,
                    request.File.FileName,
                    contentType,
                    user.Id,
                    caseId,
                    request.Description
                );

                this.Logger.LogInformation(
                    "Uploaded file {FileId} ({Size} bytes) to case {CaseId}",
                    metadata.Id, fileBytes.Length, caseId
                );

                return StatusCode(201, new SuccessResponseModel());
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex, "Failed to upload file to case {CaseId}", caseId);
                return StatusCode(500, new FailureResponseModel { Detail = "Failed to upload file" });
            }
        }

        [HttpPatch("")]
        [ProducesResponseType(typeof(CaseResponseModel), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<CaseResponseModel>> UpdateCase(
            Guid caseId,
            [FromBody] CaseUpdateRequestModel request
        )
        {
            try
            {
                var (user, error) = await GetCurrentUserAsync();
                if (error != null) return error;

                var caseEntity = await Db.Cases
                    .Include(c => c.Clients)
                    .Include(c => c.Workers)
                    .FirstOrDefaultAsync(c => c.Id == caseId);

                if (caseEntity == null)
                {
                    return NotFound(new FailureResponseModel { Detail = "Case not found" });
                }

                // only workers can update cases
                var canUpdate = user!.IsAdministrator ||
                              (caseEntity.Workers ?? []).Any(w => w.UserId == user.Id);

                if (!canUpdate)
                {
                    return StatusCode(403, new FailureResponseModel
                    {
                        Detail = "Only case workers can update cases"
                    });
                }

                // apply updates
                if (request.Title != null)
                    caseEntity.Title = request.Title;

                if (request.Description != null)
                    caseEntity.Description = request.Description;

                if (request.Status != null)
                    caseEntity.Status = request.Status ?? CaseStatusEnum.Open;

                if (request.Sensitivity != null)
                    caseEntity.Sensitivity = request.Sensitivity ?? CaseSensitivityEnum.Confidential;

                caseEntity.LastUpdatedAt = DateTime.UtcNow;
                caseEntity.LastUpdatedBy = user.Id;

                //TODO: don't use EF directly here
                await Db.SaveChangesAsync();

                this.Logger.LogInformation("Updated case {CaseId} by user {UserId}", caseId, user.Id);

                // reload relationships
                await Db.Entry(caseEntity).Collection(c => c.Files!).LoadAsync();
                await Db.Entry(caseEntity).Collection(c => c.Messages!).LoadAsync();
                await Db.Entry(caseEntity).Collection(c => c.Todos!).LoadAsync();
                await Db.Entry(caseEntity).Collection(c => c.AdditionalProperties!).LoadAsync();

                var response = ControllerUtilities.CaseMapToCaseResponseModel(caseEntity);
                return Ok(response);
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex, "Failed to update case {CaseId}", caseId);
                return StatusCode(500, new FailureResponseModel { Detail = "Failed to update case" });
            }
        }
    }
}
