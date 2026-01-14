using AuxiliumAPI.Common.ControllerBases;
using AuxiliumSoftware.AuxiliumServices.Common.DataStructures;
using AuxiliumSoftware.AuxiliumServices.Common.EF;
using AuxiliumSoftware.AuxiliumServices.Common.EntityModels;
using AuxiliumSoftware.AuxiliumServices.Common.Services.Interfaces;
using AuxiliumAPI.Models;
using AuxiliumAPI.Models.Case;
using AuxiliumAPI.Models.File;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AuxiliumSoftware.AuxiliumServices.Common.Enumerators;

namespace AuxiliumAPI.Controllers;

[ApiController]
[Route("/api/v3/cases")]
[Tags("Cases")]
[Authorize]
public class CaseController : LoggedInControllerBase
{
    private readonly ILogger<CaseController> _logger;

    private readonly ICaseDocumentService _caseDocService;
    private readonly IFileDocumentService _fileService;

    public CaseController(
        ILogger<CaseController> logger,

        ICaseDocumentService caseDocService,
        AuxiliumDbContext db,
        IFileDocumentService fileService
        )
        : base(db, logger)
    {
        _logger = logger;

        _caseDocService = caseDocService;
        _fileService = fileService;
    }

    [HttpPost("")]
    [ProducesResponseType(typeof(CaseResponseModel), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CaseResponseModel>> CreateCase(
        [FromBody] CaseCreationRequestModel request)
    {
        try
        {
            // only case workers can create cases
            var (user, error) = await RequireCaseWorkerAsync();
            if (error != null) return error;

            // create the case entity
            var caseEntity = new CaseModel
            {
                Id = Guid.NewGuid(),
                Title = request.Title,
                Description = request.Description ?? string.Empty,
                Status = CaseStatusEnum.Open,
                Sensitivity = CaseSensitivityEnum.Confidential,
                CreatedBy = user!.Id,
                CreatedAt = DateTime.UtcNow,
                LastUpdatedAt = DateTime.UtcNow,
                LastUpdatedBy = user.Id
            };

            //TODO: don't use EF directly here
            Db.Cases.Add(caseEntity);

            // add currently logged in user as a client
            Db.CaseClients.Add(new CaseClientModel
            {
                Id = Guid.NewGuid(),
                CaseId = caseEntity.Id,
                UserId = user.Id,
                CreatedBy = user.Id,
                CreatedAt = DateTime.UtcNow
            });

            await Db.SaveChangesAsync();

            _logger.LogInformation("Created case {CaseId} by user {UserId}", caseEntity.Id, user.Id);

            // build the response model
            var response = new CaseResponseModel
            {
                ID = caseEntity.Id,
                CreatedAt = caseEntity.CreatedAt,
                CreatedBy = caseEntity.CreatedBy,
                LastUpdatedAt = caseEntity.LastUpdatedAt,
                LastUpdatedBy = caseEntity.LastUpdatedBy,
                Title = caseEntity.Title,
                Description = caseEntity.Description,
                Status = caseEntity.Status,
                Sensitivity = caseEntity.Sensitivity,
                Clients = new List<Guid> { user.Id },
                Workers = new List<Guid>(),
                Files = new List<string>(),
                Messages = new List<string>(),
                Todos = new Dictionary<string, object>(),
                Timeline = new Dictionary<string, object>(),
                AdditionalProperties = new Dictionary<string, AdditionalPropertySubStructure>(),
                Referrer = null,
            };

            return CreatedAtAction(
                nameof(GetCaseById),
                new { caseId = caseEntity.Id },
                response
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create case");
            return StatusCode(500, new FailureResponseModel { Detail = "Failed to create case" });
        }
    }

    [HttpGet("mine")]
    [ProducesResponseType(typeof(PaginatedCasesResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PaginatedCasesResponseModel>> GetMyCases(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? sortBy = "createdAt",
        [FromQuery] string? sortOrder = "desc")
    {
        try
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            var query = Db.Cases
                .Include(c => c.Clients)
                .Include(c => c.Workers)
                .Include(c => c.Files)
                .Include(c => c.Messages)
                .Include(c => c.Todos)
                .Include(c => c.AdditionalProperties)
                .Where(c => c.Clients!.Any(cl => cl.UserId == user!.Id));

            query = ApplySorting(query, sortBy, sortOrder);

            var total = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(total / (double)pageSize);

            var cases = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var caseResponses = cases.Select(MapToResponseModel).ToList();

            var response = new PaginatedCasesResponseModel
            {
                Cases = caseResponses,
                Total = total,
                Page = page,
                PerPage = pageSize,
                TotalPages = totalPages,
                HasMore = page < totalPages
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch my cases");
            return StatusCode(500, new FailureResponseModel { Detail = "Failed to fetch cases" });
        }
    }

    [HttpGet("assigned")]
    [ProducesResponseType(typeof(PaginatedCasesResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PaginatedCasesResponseModel>> GetAssignedCases(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? sortBy = "createdAt",
        [FromQuery] string? sortOrder = "desc")
    {
        try
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            var query = Db.Cases
                .Include(c => c.Clients)
                .Include(c => c.Workers)
                .Include(c => c.Files)
                .Include(c => c.Messages)
                .Include(c => c.Todos)
                .Include(c => c.AdditionalProperties)
                .Where(c => c.Workers!.Any(w => w.UserId == user!.Id));

            query = ApplySorting(query, sortBy, sortOrder);

            var total = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(total / (double)pageSize);

            var cases = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var caseResponses = cases.Select(MapToResponseModel).ToList();

            var response = new PaginatedCasesResponseModel
            {
                Cases = caseResponses,
                Total = total,
                Page = page,
                PerPage = pageSize,
                TotalPages = totalPages,
                HasMore = page < totalPages
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch assigned cases");
            return StatusCode(500, new FailureResponseModel { Detail = "Failed to fetch cases" });
        }
    }

    [HttpGet("")]
    [ProducesResponseType(typeof(PaginatedCasesResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PaginatedCasesResponseModel>> SearchCases(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? sortBy = "createdAt",
        [FromQuery] string? sortOrder = "desc",
        [FromQuery] string? status = null,
        [FromQuery] string? search = null)
    {
        try
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            IQueryable<CaseModel> query;

            if (user!.IsAdmin)
            {
                // admins see all cases
                query = Db.Cases
                    .Include(c => c.Clients)
                    .Include(c => c.Workers)
                    .Include(c => c.Files)
                    .Include(c => c.Messages)
                    .Include(c => c.Todos)
                    .Include(c => c.AdditionalProperties);
            }
            else
            {
                // regular users see cases where they are client OR worker
                query = Db.Cases
                    .Include(c => c.Clients)
                    .Include(c => c.Workers)
                    .Include(c => c.Files)
                    .Include(c => c.Messages)
                    .Include(c => c.Todos)
                    .Include(c => c.AdditionalProperties)
                    .Where(c => c.Clients!.Any(cl => cl.UserId == user.Id) ||
                               c.Workers!.Any(w => w.UserId == user.Id));
            }

            // apply filters
            if (!string.IsNullOrEmpty(status))
            {
                if (Enum.TryParse<CaseStatusEnum>(status, ignoreCase: true, out var statusEnum))
                {
                    query = query.Where(c => c.Status == statusEnum);
                }
            }

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(c =>
                    (c.Title ?? "").Contains(search) ||
                    (c.Description ?? "").Contains(search));
            }

            query = ApplySorting(query, sortBy, sortOrder);

            var total = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(total / (double)pageSize);

            var cases = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var caseResponses = cases.Select(MapToResponseModel).ToList();

            var response = new PaginatedCasesResponseModel
            {
                Cases = caseResponses,
                Total = total,
                Page = page,
                PerPage = pageSize,
                TotalPages = totalPages,
                HasMore = page < totalPages
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search cases");
            return StatusCode(500, new FailureResponseModel { Detail = "Failed to fetch cases" });
        }
    }

    [HttpGet("{caseId:guid}")]
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
            var hasAccess = user!.IsAdmin ||
                          (caseEntity.Clients ?? []).Any(cl => cl.UserId == user.Id) ||
                          (caseEntity.Workers ?? []).Any(w => w.UserId == user.Id);

            if (!hasAccess)
            {
                return StatusCode(403, new FailureResponseModel
                {
                    Detail = "You don't have permission to view this case"
                });
            }

            var response = MapToResponseModel(caseEntity);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch case {CaseId}", caseId);
            return StatusCode(500, new FailureResponseModel { Detail = "Failed to fetch case" });
        }
    }

    [HttpPost("{caseId:guid}/upload")]
    [ProducesResponseType(typeof(SuccessResponseModel), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SuccessResponseModel>> UploadFile(
        Guid caseId,
        [FromForm] FileUploadRequestModel request)
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
            var canUpload = user!.IsAdmin ||
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

            _logger.LogInformation(
                "Uploaded file {FileId} ({Size} bytes) to case {CaseId}",
                metadata.Id, fileBytes.Length, caseId
            );

            return StatusCode(201, new SuccessResponseModel());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload file to case {CaseId}", caseId);
            return StatusCode(500, new FailureResponseModel { Detail = "Failed to upload file" });
        }
    }

    [HttpPatch("{caseId:guid}")]
    [ProducesResponseType(typeof(CaseResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CaseResponseModel>> UpdateCase(
        Guid caseId,
        [FromBody] CaseUpdateRequestModel request)
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

            // only workers and admins can update cases
            var canUpdate = user!.IsAdmin ||
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

            _logger.LogInformation("Updated case {CaseId} by user {UserId}", caseId, user.Id);

            // reload relationships
            await Db.Entry(caseEntity).Collection(c => c.Files!).LoadAsync();
            await Db.Entry(caseEntity).Collection(c => c.Messages!).LoadAsync();
            await Db.Entry(caseEntity).Collection(c => c.Todos!).LoadAsync();
            await Db.Entry(caseEntity).Collection(c => c.AdditionalProperties!).LoadAsync();

            var response = MapToResponseModel(caseEntity);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update case {CaseId}", caseId);
            return StatusCode(500, new FailureResponseModel { Detail = "Failed to update case" });
        }
    }
    #region ========================= HELPER METHODS =========================
    private IQueryable<CaseModel> ApplySorting(
        IQueryable<CaseModel> query,
        string? sortBy,
        string? sortOrder)
    {
        var descending = sortOrder?.ToLower() == "desc";

        return sortBy?.ToLower() switch
        {
            "createdat" => descending
                ? query.OrderByDescending(c => c.CreatedAt)
                : query.OrderBy(c => c.CreatedAt),
            "updatedat" => descending
                ? query.OrderByDescending(c => c.LastUpdatedAt)
                : query.OrderBy(c => c.LastUpdatedAt),
            "title" => descending
                ? query.OrderByDescending(c => c.Title)
                : query.OrderBy(c => c.Title),
            "status" => descending
                ? query.OrderByDescending(c => c.Status)
                : query.OrderBy(c => c.Status),
            _ => query.OrderByDescending(c => c.CreatedAt)
        };
    }

    private CaseResponseModel MapToResponseModel(CaseModel caseEntity)
    {
        return new CaseResponseModel
        {
            ID = caseEntity.Id,
            CreatedAt = caseEntity.CreatedAt,
            CreatedBy = caseEntity.CreatedBy,
            LastUpdatedAt = caseEntity.LastUpdatedAt,
            LastUpdatedBy = caseEntity.LastUpdatedBy,

            Title = caseEntity.Title,
            Description = caseEntity.Description,
            Status = caseEntity.Status,
            Sensitivity = caseEntity.Sensitivity,

            Clients = caseEntity.Clients?.Select(c => c.UserId).ToList() ?? new List<Guid>(),
            Workers = caseEntity.Workers?.Select(w => w.UserId).ToList() ?? new List<Guid>(),

            Files = caseEntity.Files?.Select(f => $"auxlfs://localhost/files/{f.Id}").ToList() ?? new List<string>(),
            Messages = caseEntity.Messages?.Select(m => $"auxmsg://localhost/message/{m.Id}").ToList() ?? new List<string>(),

            Referrer = null,

            // Map todos from entities
            Todos = caseEntity.Todos?
                .ToDictionary(
                    t => t.Id.ToString(),
                    t => (object)new
                    {
                        id = t.Id,
                        summary = t.Summary,
                        description = t.Description,
                        status = t.Status.ToString(),
                        priority = t.Priority.ToString(),
                        due_date = t.DueDate,
                        assigned_to = t.AssignedTo,
                        completed_at = t.CompletedAt
                    }
                ) ?? new Dictionary<string, object>(),

            Timeline = caseEntity.Timeline?
                .ToDictionary(
                    t => t.Id.ToString(),
                    t => (object)new
                    {
                        id = t.Id,
                    }
                ) ?? new Dictionary<string, object>(),

            AdditionalProperties = caseEntity.AdditionalProperties?
                .ToDictionary(
                    p => p.Name,
                    p => new AdditionalPropertySubStructure
                    {
                        Id = p.Id,
                        CreatedAt = p.CreatedAt,
                        CreatedBy = p.CreatedBy,
                        UpdatedAt = p.LastUpdatedAt,
                        LastUpdatedBy = p.LastUpdatedBy,
                        OriginalName = p.Name,
                        PrettyName = p.Name,
                        UrlSlug = p.Name.ToLower().Replace(" ", "-"),
                        Content = p.Content,
                        ContentType = p.ContentType
                    }
                ) ?? new Dictionary<string, AdditionalPropertySubStructure>(),
            };
    }
    #endregion
}
