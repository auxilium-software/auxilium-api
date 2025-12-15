using AuxiliumAPI.Common.ControllerBases;
using AuxiliumAPI.Common.DataStructures.CouchDB;
using AuxiliumAPI.Common.DataStructures.CouchDB.SubStructures;
using AuxiliumAPI.Common.DataStructures.MariaDB;
using AuxiliumAPI.Common.Enumerators;
using AuxiliumAPI.Common.Services;
using AuxiliumAPI.Common.Services.Interfaces;
using AuxiliumAPI.Common.Utilities;
using AuxiliumAPI.Models;
using AuxiliumAPI.Models.Case;
using AuxiliumAPI.Models.File;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AuxiliumAPI.Controllers;

[ApiController]
[Route("/api/v3/cases")]
[Tags("Cases")]
[Authorize]
public class CaseController : LoggedInControllerBase
{
    private readonly IConfiguration Configuration;
    private readonly ILogger<CaseController> _logger;
    private readonly ICouchDbService _couchDb;
    private readonly IMariaDbService _mariaDb;

    public CaseController(
        IConfiguration configuration,
        ILogger<CaseController> logger,
        ICouchDbService couchDb,
        IMariaDbService mariaDb
        ) : base(mariaDb, logger)
    {
        this.Configuration = configuration;
        this._logger = logger;
        this._couchDb = couchDb;
        this._mariaDb = mariaDb;
    }

    [HttpPost("")]
    [ProducesResponseType(typeof(CaseResponseModel), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CaseResponseModel>> CreateCase(
        [FromBody] CaseCreationRequestModel request)
    {
        try
        {
            // enforce login and get current user details
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            // generate new case id
            Guid caseId = UUIDUtilities.GenerateV5(DatabaseObjectType.Case);

            // create the case document
            var caseDoc = new CaseDocumentStructure
            {
                Id = caseId.ToString(),
                CreatedBy = user.id,
                CreatedAt = DateTime.UtcNow,

                Title = request.Title,
                Description = request.Description,
                Sensitivity = CaseSensitivityEnum.Confidential,
                Status = CaseStatusEnum.Open,

                Clients = new List<Guid> { user.id },
                Workers = new List<Guid>(),

                Files = new List<string>(),
                Messages = new List<string>(),

                AdditionalProperties = new Dictionary<string, AdditionalPropertyStructure>()
            };

            // save to couchdb
            await _couchDb.SaveDocumentAsync(
                this.Configuration!["Databases:CouchDB:Databases:Cases"]!,
                caseDoc
            );

            // build newly created case into a response model
            var response = new CaseResponseModel
            {
                ID = Guid.Parse(caseDoc.Id),
                CreatedAt = caseDoc.CreatedAt,
                CreatedBy = caseDoc.CreatedBy,
                LastUpdatedAt = caseDoc.LastUpdatedAt,
                LastUpdatedBy = caseDoc.LastUpdatedBy,

                Title = caseDoc.Title,
                Description = caseDoc.Description,

                Sensitivity = caseDoc.Sensitivity,
                Status = caseDoc.Status,

                Clients = caseDoc.Clients,
                Workers = caseDoc.Workers,

                Referrer = caseDoc.Referrer,

                Todos = caseDoc.Todos,
                Timeline = caseDoc.Timeline,
                Files = caseDoc.Files,
                Messages = caseDoc.Messages,

                AdditionalProperties = caseDoc.AdditionalProperties,
            };

            // return
            return CreatedAtAction(
                nameof(GetCaseById),
                new { caseId = caseDoc.Id },
                response
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create case");
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new FailureResponseModel { Detail = $"Failed to create case: {ex.Message}" }
            );
        }
    }

    [HttpGet("mine")]
    [ProducesResponseType(typeof(PaginatedCasesResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PaginatedCasesResponseModel>> GetMyCases(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? sortBy = "createdAt",
        [FromQuery] string? sortOrder = "desc"
        )
    {
        try
        {
            // enforce login and get current user details
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            // selector: cases where user is in clients array
            var selector = new
            {
                clients = new
                {
                    elemMatch = new { eq = user.id }
                }
            };

            var skip = (page - 1) * pageSize;

            // query couchdb
            var result = await _couchDb.QueryAsync<CaseDocumentStructure>(
                this.Configuration!["Databases:CouchDB:Databases:Cases"]!,
                selector,
                limit: pageSize,
                skip: skip,
                sort: [$"{{{sortBy}:\"{sortOrder}\"}}"]
            );

            // get total count
            var total = await _couchDb.CountAsync<CaseDocumentStructure>(
                this.Configuration!["Databases:CouchDB:Databases:Cases"]!,
                selector
            );
            var totalPages = (int)Math.Ceiling(total / (double)pageSize);

            // populate response models
            var cases = result.Documents.Select(caseDoc => new CaseResponseModel
            {
                ID = Guid.Parse(caseDoc.Id),
                CreatedAt = caseDoc.CreatedAt,
                CreatedBy = caseDoc.CreatedBy,
                LastUpdatedAt = caseDoc.LastUpdatedAt,
                LastUpdatedBy = caseDoc.LastUpdatedBy,

                Title = caseDoc.Title,
                Description = caseDoc.Description,

                Sensitivity = caseDoc.Sensitivity,
                Status = caseDoc.Status,

                Clients = caseDoc.Clients,
                Workers = caseDoc.Workers,

                Referrer = caseDoc.Referrer,
                Todos = caseDoc.Todos,
                Timeline = caseDoc.Timeline,
                Messages = caseDoc.Messages,

                AdditionalProperties = caseDoc.AdditionalProperties,
                Files = caseDoc.Files,
            }).ToList();

            // build paginated response model
            var response = new PaginatedCasesResponseModel
            {
                Cases = cases,
                Total = total,
                Page = page,
                PerPage = pageSize,
                TotalPages = totalPages,
                HasMore = page < totalPages,
            };

            // return
            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new FailureResponseModel { Detail = $"Failed to fetch cases: {ex.Message}" }
            );
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
            // enforce login and get current user details
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            // selector: cases where user is in workers array
            var selector = new
            {
                workers = new
                {
                    elemMatch = new { eq = user.id }
                }
            };

            // skip a "page"
            var skip = (page - 1) * pageSize;

            // query couchdb
            var result = await _couchDb.QueryAsync<CaseDocumentStructure>(
                this.Configuration!["Databases:CouchDB:Databases:Cases"]!,
                selector,
                limit: pageSize,
                skip: skip,
                sort: new[] { $"{{{sortBy}:\"{sortOrder}\"}}" }
            );

            // get total count
            var total = await _couchDb.CountAsync<CaseDocumentStructure>(
                this.Configuration!["Databases:CouchDB:Databases:Cases"]!,
                selector
            );
            var totalPages = (int)Math.Ceiling(total / (double)pageSize);

            // populate response models
            var cases = result.Documents.Select(caseDoc => new CaseResponseModel
            {
                ID = Guid.Parse(caseDoc.Id),
                CreatedAt = caseDoc.CreatedAt,
                CreatedBy = caseDoc.CreatedBy,
                LastUpdatedAt = caseDoc.LastUpdatedAt,
                LastUpdatedBy = caseDoc.LastUpdatedBy,

                Title = caseDoc.Title,
                Description = caseDoc.Description,

                Sensitivity = caseDoc.Sensitivity,
                Status = caseDoc.Status,

                Clients = caseDoc.Clients,
                Workers = caseDoc.Workers,

                Referrer = caseDoc.Referrer,
                Todos = caseDoc.Todos,
                Timeline = caseDoc.Timeline,
                Messages = caseDoc.Messages,

                AdditionalProperties = caseDoc.AdditionalProperties,
                Files = caseDoc.Files,
            }).ToList();

            // build paginated response model
            var response = new PaginatedCasesResponseModel
            {
                Cases = cases,
                Total = total,
                Page = page,
                PerPage = pageSize,
                TotalPages = totalPages,
                HasMore = page < totalPages,
            };

            // return
            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new FailureResponseModel { Detail = $"Failed to fetch cases: {ex.Message}" }
            );
        }
    }

    [HttpGet("")]
    [ProducesResponseType(typeof(PaginatedCasesResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PaginatedCasesResponseModel>> SearchCases(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? sortBy = "createdAt",
        [FromQuery] string? sortOrder = "desc")
    {
        try
        {
            // enforce login and get current user details
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            // check if user is admin
            object selector;

            if (user.is_admin)
            {
                // admins see all cases
                selector = new { };
            }
            else
            {
                // regular users see cases where they are client OR worker
                selector = new
                {
                    or = new object[]
                    {
                        new { clients = new { elemMatch = new { eq = user.id } } },
                        new { workers = new { elemMatch = new { eq = user.id } } }
                    }
                };
            }

            // skip a "page"
            var skip = (page - 1) * pageSize;

            // query couchdb
            var result = await _couchDb.QueryAsync<CaseDocumentStructure>(
                this.Configuration!["Databases:CouchDB:Databases:Cases"]!,
                selector,
                limit: pageSize,
                skip: skip,
                sort: [$"{{{sortBy}:\"{sortOrder}\"}}"]
            );
            var total = await _couchDb.CountAsync<CaseDocumentStructure>(
                this.Configuration!["Databases:CouchDB:Databases:Cases"]!,
                selector
            );
            var totalPages = (int)Math.Ceiling(total / (double)pageSize);

            // build response models
            var cases = result.Documents.Select(caseDoc => new CaseResponseModel
            {
                ID = Guid.Parse(caseDoc.Id),
                CreatedAt = caseDoc.CreatedAt,
                CreatedBy = caseDoc.CreatedBy,
                LastUpdatedAt = caseDoc.LastUpdatedAt,
                LastUpdatedBy = caseDoc.LastUpdatedBy,

                Title = caseDoc.Title,
                Description = caseDoc.Description,

                Sensitivity = caseDoc.Sensitivity,
                Status = caseDoc.Status,

                Clients = caseDoc.Clients,
                Workers = caseDoc.Workers,

                Referrer = caseDoc.Referrer,
                Todos = caseDoc.Todos,
                Timeline = caseDoc.Timeline,
                Messages = caseDoc.Messages,

                AdditionalProperties = caseDoc.AdditionalProperties,
                Files = caseDoc.Files,
            }).ToList();
            var response = new PaginatedCasesResponseModel
            {
                Cases = cases,
                Total = total,
                Page = page,
                PerPage = pageSize,
                TotalPages = totalPages,
                HasMore = page < totalPages,
            };

            // return
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search cases");
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new FailureResponseModel { Detail = $"Failed to fetch cases: {ex.Message}" }
            );
        }
    }

    [HttpPost("{caseId}/upload")]
    [ProducesResponseType(typeof(SuccessResponseModel), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SuccessResponseModel>> UploadFile(
        string caseId,
        [FromForm] FileUploadRequestModel request)
    {
        try
        {
            // enforce login and get current user details
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            // grab the case document from couchdb
            if (!Guid.TryParse(caseId, out _)) return BadRequest(new FailureResponseModel() { Detail = "You must provide a valid UUID" });
            var caseDoc = await _couchDb.GetDocumentAsync<CaseDocumentStructure>(
                this.Configuration!["Databases:CouchDB:Databases:Cases"]!,
                Guid.Parse(caseId)
            );
            if (caseDoc == null) return NotFound(new FailureResponseModel() { Detail = "Case not found" });

            // check there is a file
            if (request.File == null || request.File.Length == 0)
            {
                return BadRequest(new { detail = "No file provided" });
            }

            // read file contents
            using var memoryStream = new MemoryStream();
            await request.File.CopyToAsync(memoryStream);
            var fileBytes = memoryStream.ToArray();


            var contentType = request.File.ContentType ?? "application/octet-stream";
            Guid fileId = UUIDUtilities.GenerateV5(DatabaseObjectType.File);
            var fileHash = HashingUtilities.SHA256Hash(fileBytes);

            // build document structure for the file
            var fileStructure = new FileDocumentStructure
            {
                Id = fileId.ToString(),
                Filename = request.File.FileName,
                Description = request.Description,
                ContentType = contentType,
                Hash = fileHash,
                Size = fileBytes.Length,
                CreatedBy = user.id,
                CreatedAt = DateTime.UtcNow,
            };

            // update the case document to include a reference to the file
            caseDoc.Files ??= [];
            caseDoc.Files.Add($"auxlfs://localhost/file/{fileId}?size={fileBytes.Length}&hash={fileHash}");
            caseDoc.LastUpdatedAt = DateTime.UtcNow;
            caseDoc.LastUpdatedBy = user.id;

            // save file to lfs
            string path = this.Configuration["FileSystem:RootStorageDirectories:AuxLFS"] + $"/{fileId}.bin";
            System.IO.File.WriteAllBytes(path, fileBytes);

            // save the updated case document
            await _couchDb.SaveDocumentAsync(
                this.Configuration!["Databases:CouchDB:Databases:Cases"]!,
                caseDoc
            );
            await _couchDb.SaveDocumentAsync(
                this.Configuration!["Databases:CouchDB:Databases:Files"]!,
                fileStructure
            );

            // return
            return StatusCode(201, new SuccessResponseModel());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload file");
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new FailureResponseModel { Detail = $"Failed to upload file" }
            );
        }
    }

    [HttpGet("{caseId}")]
    [ProducesResponseType(typeof(CaseResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CaseResponseModel>> GetCaseById(string caseId)
    {
        try
        {
            // enforce login and get current user details
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            // grab the case document from couchdb
            if (!Guid.TryParse(caseId, out _)) return BadRequest(new FailureResponseModel() { Detail = "You must provide a valid UUID" });
            var caseDoc = await _couchDb.GetDocumentAsync<CaseDocumentStructure>(
                this.Configuration!["Databases:CouchDB:Databases:Cases"]!,
                Guid.Parse(caseId)
            );
            if (caseDoc == null) return NotFound(new FailureResponseModel() { Detail = "Case not found" });

            // build response model
            var response = new CaseResponseModel
            {
                ID = Guid.Parse(caseId),
                CreatedAt = caseDoc.CreatedAt,
                CreatedBy = caseDoc.CreatedBy,
                LastUpdatedAt = caseDoc.LastUpdatedAt,
                LastUpdatedBy = caseDoc.LastUpdatedBy,

                Title = caseDoc.Title,
                Description = caseDoc.Description,

                Sensitivity = caseDoc.Sensitivity,
                Status = caseDoc.Status,

                Clients = caseDoc.Clients,
                Workers = caseDoc.Workers,

                Referrer = caseDoc.Referrer,
                Todos = caseDoc.Todos,
                Timeline = caseDoc.Timeline,
                Messages = caseDoc.Messages,

                AdditionalProperties = caseDoc.AdditionalProperties,
                Files = caseDoc.Files,
            };

            // return
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch case {CaseId}", caseId);
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new FailureResponseModel { Detail = $"Failed to fetch case: {ex.Message}" }
            );
        }
    }

    [HttpPatch("{caseId}")]
    [ProducesResponseType(typeof(CaseResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CaseResponseModel>> UpdateCase(
        string caseId,
        [FromBody] CaseUpdateRequestModel request)
    {
        try
        {
            // enforce login and get current user details
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            // grab the case document from couchdb
            if (!Guid.TryParse(caseId, out _)) return BadRequest(new FailureResponseModel() { Detail = "You must provide a valid UUID" });
            var caseDoc = await _couchDb.GetDocumentAsync<CaseDocumentStructure>(
                this.Configuration!["Databases:CouchDB:Databases:Cases"]!,
                Guid.Parse(caseId)
            );
            if (caseDoc == null) return NotFound(new FailureResponseModel() { Detail = "Case not found" });

            // check to see if anything needs updating
            if (request.Title != null)
                caseDoc.Title = request.Title;
            if (request.Description != null)
                caseDoc.Description = request.Description;

            // update LastUpdated{At,By} fields
            caseDoc.LastUpdatedAt = DateTime.UtcNow;
            caseDoc.LastUpdatedBy = user.id;

            // save the updated case document
            await _couchDb.SaveDocumentAsync(
                this.Configuration!["Databases:CouchDB:Databases:Cases"]!,
                caseDoc
            );

            // build response model
            var response = new CaseResponseModel
            {
                ID = Guid.Parse(caseDoc.Id),
                CreatedAt = caseDoc.CreatedAt,
                CreatedBy = caseDoc.CreatedBy,
                LastUpdatedAt = caseDoc.LastUpdatedAt,
                LastUpdatedBy = caseDoc.LastUpdatedBy,

                Title = caseDoc.Title,
                Description = caseDoc.Description,

                Sensitivity = caseDoc.Sensitivity,
                Status = caseDoc.Status,

                Clients = caseDoc.Clients,
                Workers = caseDoc.Workers,

                Referrer = caseDoc.Referrer,
                Todos = caseDoc.Todos,
                Timeline = caseDoc.Timeline,
                Messages = caseDoc.Messages,

                AdditionalProperties = caseDoc.AdditionalProperties,
                Files = caseDoc.Files,
            };

            // return
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update case {CaseId}", caseId);
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new FailureResponseModel{ Detail = $"Failed to update case: {ex.Message}" }
            );
        }
    }
}
