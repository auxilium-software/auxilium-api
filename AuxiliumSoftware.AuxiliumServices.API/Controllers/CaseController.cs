using AuxiliumSoftware.AuxiliumServices.API.Common.ControllerBases;
using AuxiliumSoftware.AuxiliumServices.API.Common.Utilities;
using AuxiliumSoftware.AuxiliumServices.API.Models;
using AuxiliumSoftware.AuxiliumServices.API.Models.Case;
using AuxiliumSoftware.AuxiliumServices.API.Models.File;
using AuxiliumSoftware.AuxiliumServices.Common.DataTransferObjects;
using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework;
using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework.EntityModels;
using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework.Enumerators;
using AuxiliumSoftware.AuxiliumServices.Common.Enumerators;
using AuxiliumSoftware.AuxiliumServices.Common.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuxiliumSoftware.AuxiliumServices.API.Controllers;

[ApiController]
[Route("/api/v3/cases")]
[Tags("Cases")]
[Authorize]
public class CaseController : LoggedInControllerBase
{
    private readonly ICaseDocumentService _caseDocService;
    private readonly IFileDocumentService _fileService;

    public CaseController(
        ISystemSettingsService systemSettingsService,
        IConfiguration configuration,
        AuxiliumDbContext db,
        IWebApplicationFirewallService waf,
        ILogger<CaseController> logger,
        ITotpService totpService,

        ICaseDocumentService caseDocService,
        IFileDocumentService fileService
        )
        : base(systemSettingsService, configuration, db, waf, logger, totpService)
    {
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
            var caseEntity = new CaseEntityModel
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
            Db.CaseClients.Add(new CaseClientEntityModel
            {
                Id = Guid.NewGuid(),
                CaseId = caseEntity.Id,
                UserId = user.Id,
                CreatedBy = user.Id,
                CreatedAt = DateTime.UtcNow
            });

            await Db.SaveChangesAsync();

            this.Logger.LogInformation("Created case {CaseId} by user {UserId}", caseEntity.Id, user.Id);

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
                AdditionalProperties = new Dictionary<string, AdditionalPropertySubStructureDTO>(),
                Referrer = null,
            };

            return StatusCode(StatusCodes.Status201Created, response);
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Failed to create case");
            return StatusCode(StatusCodes.Status500InternalServerError, new FailureResponseModel { Detail = "Failed to create case" });
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

            query = ControllerUtilities.ApplySortingForCases(query, sortBy, sortOrder);

            var total = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(total / (double)pageSize);

            var cases = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var caseResponses = cases.Select(ControllerUtilities.CaseMapToCaseResponseModel).ToList();

            var response = new PaginatedCasesResponseModel
            {
                Cases = caseResponses,
                Total = total,
                Page = page,
                PerPage = pageSize,
                TotalPages = totalPages,
                HasMore = page < totalPages
            };

            return StatusCode(StatusCodes.Status200OK, response);
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Failed to fetch my cases");
            return StatusCode(StatusCodes.Status500InternalServerError, new FailureResponseModel { Detail = "Failed to fetch cases" });
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

            query = ControllerUtilities.ApplySortingForCases(query, sortBy, sortOrder);

            var total = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(total / (double)pageSize);

            var cases = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var caseResponses = cases.Select(ControllerUtilities.CaseMapToCaseResponseModel).ToList();

            var response = new PaginatedCasesResponseModel
            {
                Cases = caseResponses,
                Total = total,
                Page = page,
                PerPage = pageSize,
                TotalPages = totalPages,
                HasMore = page < totalPages
            };

            return StatusCode(StatusCodes.Status200OK, response);
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Failed to fetch assigned cases");
            return StatusCode(StatusCodes.Status500InternalServerError, new FailureResponseModel { Detail = "Failed to fetch cases" });
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
        [FromQuery] string? search = null
        )
    {
        try
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            IQueryable<CaseEntityModel> query;

            if (user!.IsAdministrator)
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

            query = ControllerUtilities.ApplySortingForCases(query, sortBy, sortOrder);

            var total = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(total / (double)pageSize);

            var cases = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var caseResponses = cases.Select(ControllerUtilities.CaseMapToCaseResponseModel).ToList();

            var response = new PaginatedCasesResponseModel
            {
                Cases = caseResponses,
                Total = total,
                Page = page,
                PerPage = pageSize,
                TotalPages = totalPages,
                HasMore = page < totalPages
            };

            return StatusCode(StatusCodes.Status200OK, response);
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Failed to search cases");
            return StatusCode(StatusCodes.Status500InternalServerError, new FailureResponseModel { Detail = "Failed to fetch cases" });
        }
    }
}
