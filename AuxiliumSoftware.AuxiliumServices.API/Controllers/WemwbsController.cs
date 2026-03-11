using AuxiliumSoftware.AuxiliumServices.API.Common.ControllerBases;
using AuxiliumSoftware.AuxiliumServices.API.Models;
using AuxiliumSoftware.AuxiliumServices.API.Models.WEMWBS;
using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework;
using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework.EntityModels;
using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework.Enumerators;
using AuxiliumSoftware.AuxiliumServices.Common.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuxiliumSoftware.AuxiliumServices.API.Controllers;

[ApiController]
[Route("/api/v3/wemwbs")]
[Tags("WEMWBS")]
public class WEMWBSController : LoggedInControllerBase
{
    public WEMWBSController(
        ISystemSettingsService systemSettingsService,
        IConfiguration configuration,
        AuxiliumDbContext db,
        IWebApplicationFirewallService waf,
        ITotpService totpService,

        ILogger<WEMWBSController> logger
    )
        : base(systemSettingsService, configuration, db, waf, logger, totpService)
    {
    }

    /// <summary>
    /// Get all WEMWBS assessments for the authenticated user
    /// </summary>
    [HttpGet("")]
    [ProducesResponseType(typeof(PaginatedWEMWBSResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PaginatedWEMWBSResponseModel>> GetAllAssessments(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? sortBy = "createdAt",
        [FromQuery] string? sortOrder = "desc"
        )
    {
        try
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            var query = Db.WemwbsAssessments.AsQueryable();

            // apply sorting
            query = ApplySorting(query, sortBy, sortOrder);

            var total = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(total / (double)pageSize);

            var assessments = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var assessmentResponses = assessments.Select(a => new WEMWBSResponseModel
            {
                Id = a.Id,
                CreatedAt = a.CreatedAt,
                CreatedBy = a.CreatedBy,
                TotalScore = CalculateTotalScore(a),
                Scores = new WEMWBSScoresModel
                {
                    OptimismScore = a.OptimismScore,
                    UsefulnessScore = a.UsefulnessScore,
                    RelaxedScore = a.RelaxedScore,
                    InterestedInPeopleScore = a.InterestedInPeopleScore,
                    SpareEnergyScore = a.SpareEnergyScore,
                    ProblemHandlingScore = a.ProblemHandlingScore,
                    ClearThoughtScore = a.ClearThoughtScore,
                    FeelingGoodSelfScore = a.FeelingGoodSelfScore,
                    FeelingCloseToPeopleScore = a.FeelingCloseToPeopleScore,
                    ConfidenceScore = a.ConfidenceScore,
                    MakingUpOwnMindScore = a.MakingUpOwnMindScore,
                    FeelingLovedScore = a.FeelingLovedScore,
                    InterestedInNewThingsScore = a.InterestedInNewThingsScore,
                    FeelingCheerfulScore = a.FeelingCheerfulScore
                }
            }).ToList();

            var response = new PaginatedWEMWBSResponseModel
            {
                Assessments = assessmentResponses,
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
            this.Logger.LogError(ex, "Failed to fetch WEMWBS assessments");
            return StatusCode(500, new FailureResponseModel
            {
                Detail = "Failed to fetch WEMWBS assessments"
            });
        }
    }

    /// <summary>
    /// Get a specific WEMWBS assessment by ID
    /// </summary>
    [HttpGet("{assessmentId:guid}")]
    [ProducesResponseType(typeof(WEMWBSResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<WEMWBSResponseModel>> GetAssessment(Guid assessmentId)
    {
        try
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            var assessment = await Db.WemwbsAssessments
                .FirstOrDefaultAsync(w => w.Id == assessmentId);

            if (assessment == null)
            {
                return NotFound(new FailureResponseModel { Detail = "Assessment not found" });
            }

            // only allow user to see their own assessments (unless admin)
            if (assessment.CreatedBy != user!.Id && !user.IsAdministrator)
            {
                return NotFound(new FailureResponseModel { Detail = "Assessment not found" });
            }

            var response = new WEMWBSResponseModel
            {
                Id = assessment.Id,
                CreatedAt = assessment.CreatedAt,
                CreatedBy = assessment.CreatedBy,
                TotalScore = CalculateTotalScore(assessment),
                Scores = new WEMWBSScoresModel
                {
                    OptimismScore = assessment.OptimismScore,
                    UsefulnessScore = assessment.UsefulnessScore,
                    RelaxedScore = assessment.RelaxedScore,
                    InterestedInPeopleScore = assessment.InterestedInPeopleScore,
                    SpareEnergyScore = assessment.SpareEnergyScore,
                    ProblemHandlingScore = assessment.ProblemHandlingScore,
                    ClearThoughtScore = assessment.ClearThoughtScore,
                    FeelingGoodSelfScore = assessment.FeelingGoodSelfScore,
                    FeelingCloseToPeopleScore = assessment.FeelingCloseToPeopleScore,
                    ConfidenceScore = assessment.ConfidenceScore,
                    MakingUpOwnMindScore = assessment.MakingUpOwnMindScore,
                    FeelingLovedScore = assessment.FeelingLovedScore,
                    InterestedInNewThingsScore = assessment.InterestedInNewThingsScore,
                    FeelingCheerfulScore = assessment.FeelingCheerfulScore
                }
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Failed to fetch WEMWBS assessment {AssessmentId}", assessmentId);
            return StatusCode(500, new FailureResponseModel
            {
                Detail = "Failed to fetch WEMWBS assessment"
            });
        }
    }

    /// <summary>
    /// Create a new WEMWBS assessment
    /// </summary>
    [HttpPost("")]
    [ProducesResponseType(typeof(WEMWBSResponseModel), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<WEMWBSResponseModel>> CreateAssessment([FromBody] CreateWEMWBSRequestModel request)
    {
        try
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            // validate scores (1-5 range for each)
            var scores = new[]
            {
                request.OptimismScore,
                request.UsefulnessScore,
                request.RelaxedScore,
                request.InterestedInPeopleScore,
                request.SpareEnergyScore,
                request.ProblemHandlingScore,
                request.ClearThoughtScore,
                request.FeelingGoodSelfScore,
                request.FeelingCloseToPeopleScore,
                request.ConfidenceScore,
                request.MakingUpOwnMindScore,
                request.FeelingLovedScore,
                request.InterestedInNewThingsScore,
                request.FeelingCheerfulScore
            };

            if (scores.Any(s => s < 1 || s > 5))
            {
                return BadRequest(new FailureResponseModel
                {
                    Detail = "All scores must be between 1 and 5"
                });
            }

            var assessment = new WemwbsAssessmentEntityModel
            {
                Id = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,
                CreatedBy = user!.Id,
                OptimismScore = request.OptimismScore,
                UsefulnessScore = request.UsefulnessScore,
                RelaxedScore = request.RelaxedScore,
                InterestedInPeopleScore = request.InterestedInPeopleScore,
                SpareEnergyScore = request.SpareEnergyScore,
                ProblemHandlingScore = request.ProblemHandlingScore,
                ClearThoughtScore = request.ClearThoughtScore,
                FeelingGoodSelfScore = request.FeelingGoodSelfScore,
                FeelingCloseToPeopleScore = request.FeelingCloseToPeopleScore,
                ConfidenceScore = request.ConfidenceScore,
                MakingUpOwnMindScore = request.MakingUpOwnMindScore,
                FeelingLovedScore = request.FeelingLovedScore,
                InterestedInNewThingsScore = request.InterestedInNewThingsScore,
                FeelingCheerfulScore = request.FeelingCheerfulScore
            };

            Db.WemwbsAssessments.Add(assessment);
            await Db.SaveChangesAsync();

            var response = new WEMWBSResponseModel
            {
                Id = assessment.Id,
                CreatedAt = assessment.CreatedAt,
                CreatedBy = assessment.CreatedBy,
                TotalScore = CalculateTotalScore(assessment),
                Scores = new WEMWBSScoresModel
                {
                    OptimismScore = assessment.OptimismScore,
                    UsefulnessScore = assessment.UsefulnessScore,
                    RelaxedScore = assessment.RelaxedScore,
                    InterestedInPeopleScore = assessment.InterestedInPeopleScore,
                    SpareEnergyScore = assessment.SpareEnergyScore,
                    ProblemHandlingScore = assessment.ProblemHandlingScore,
                    ClearThoughtScore = assessment.ClearThoughtScore,
                    FeelingGoodSelfScore = assessment.FeelingGoodSelfScore,
                    FeelingCloseToPeopleScore = assessment.FeelingCloseToPeopleScore,
                    ConfidenceScore = assessment.ConfidenceScore,
                    MakingUpOwnMindScore = assessment.MakingUpOwnMindScore,
                    FeelingLovedScore = assessment.FeelingLovedScore,
                    InterestedInNewThingsScore = assessment.InterestedInNewThingsScore,
                    FeelingCheerfulScore = assessment.FeelingCheerfulScore
                }
            };

            return CreatedAtAction(nameof(GetAssessment), new { assessmentId = assessment.Id }, response);
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Failed to create WEMWBS assessment");
            return StatusCode(500, new FailureResponseModel
            {
                Detail = "Failed to create WEMWBS assessment"
            });
        }
    }

    /// <summary>
    /// Get WEMWBS assessments for a specific user (admin only)
    /// </summary>
    [HttpGet("user/{targetUserId:guid}")]
    [ProducesResponseType(typeof(PaginatedWEMWBSResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PaginatedWEMWBSResponseModel>> GetUserAssessments(
        Guid targetUserId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? sortBy = "createdAt",
        [FromQuery] string? sortOrder = "desc")
    {
        try
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            if (!user!.IsAdministrator)
            {
                return StatusCode(403, new FailureResponseModel
                {
                    Detail = "Forbidden"
                });
            }

            var query = Db.WemwbsAssessments
                .Where(w => w.CreatedBy == targetUserId);

            // apply sorting
            query = ApplySorting(query, sortBy, sortOrder);

            var total = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(total / (double)pageSize);

            var assessments = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var assessmentResponses = assessments.Select(a => new WEMWBSResponseModel
            {
                Id = a.Id,
                CreatedAt = a.CreatedAt,
                CreatedBy = a.CreatedBy,
                TotalScore = CalculateTotalScore(a),
                Scores = new WEMWBSScoresModel
                {
                    OptimismScore = a.OptimismScore,
                    UsefulnessScore = a.UsefulnessScore,
                    RelaxedScore = a.RelaxedScore,
                    InterestedInPeopleScore = a.InterestedInPeopleScore,
                    SpareEnergyScore = a.SpareEnergyScore,
                    ProblemHandlingScore = a.ProblemHandlingScore,
                    ClearThoughtScore = a.ClearThoughtScore,
                    FeelingGoodSelfScore = a.FeelingGoodSelfScore,
                    FeelingCloseToPeopleScore = a.FeelingCloseToPeopleScore,
                    ConfidenceScore = a.ConfidenceScore,
                    MakingUpOwnMindScore = a.MakingUpOwnMindScore,
                    FeelingLovedScore = a.FeelingLovedScore,
                    InterestedInNewThingsScore = a.InterestedInNewThingsScore,
                    FeelingCheerfulScore = a.FeelingCheerfulScore
                }
            }).ToList();

            var response = new PaginatedWEMWBSResponseModel
            {
                Assessments = assessmentResponses,
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
            this.Logger.LogError(ex, "Failed to fetch user WEMWBS assessments for user {UserId}", targetUserId);
            return StatusCode(500, new FailureResponseModel
            {
                Detail = "Failed to fetch user WEMWBS assessments"
            });
        }
    }

    /// <summary>
    /// Delete a WEMWBS assessment (admin only)
    /// </summary>
    [HttpDelete("{assessmentId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteAssessment(Guid assessmentId)
    {
        try
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            if (!user!.IsAdministrator)
            {
                return StatusCode(403, new FailureResponseModel
                {
                    Detail = "Forbidden"
                });
            }

            var assessment = await Db.WemwbsAssessments.FindAsync(assessmentId);

            if (assessment == null)
            {
                return NotFound(new FailureResponseModel { Detail = "Assessment not found" });
            }

            Db.WemwbsAssessments.Remove(assessment);
            await Db.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Failed to delete WEMWBS assessment {AssessmentId}", assessmentId);
            return StatusCode(500, new FailureResponseModel
            {
                Detail = "Failed to delete WEMWBS assessment"
            });
        }
    }

    /// <summary>
    /// Get wellbeing statistics for the authenticated user
    /// </summary>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(WEMWBSStatisticsResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<WEMWBSStatisticsResponseModel>> GetMyStatistics()
    {
        try
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            var assessments = await Db.WemwbsAssessments
                .Where(w => w.CreatedBy == user!.Id)
                .OrderBy(w => w.CreatedAt)
                .ToListAsync();

            if (!assessments.Any())
            {
                return Ok(new WEMWBSStatisticsResponseModel
                {
                    TotalAssessments = 0,
                    AverageScore = 0,
                    LowestScore = 0,
                    HighestScore = 0,
                    LatestScore = 0,
                    Trend = "N/A"
                });
            }

            var scores = assessments.Select(CalculateTotalScore).ToList();
            var latestScore = scores.Last();
            var trend = "Stable";

            if (scores.Count >= 2)
            {
                var previousScore = scores[^2];
                if (latestScore > previousScore + 3) trend = "Improving";
                else if (latestScore < previousScore - 3) trend = "Declining";
            }

            return Ok(new WEMWBSStatisticsResponseModel
            {
                TotalAssessments = assessments.Count,
                AverageScore = scores.Average(),
                LowestScore = scores.Min(),
                HighestScore = scores.Max(),
                LatestScore = latestScore,
                Trend = trend
            });
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Failed to fetch WEMWBS statistics");
            return StatusCode(500, new FailureResponseModel
            {
                Detail = "Failed to fetch WEMWBS statistics"
            });
        }
    }












    private static int CalculateTotalScore(WemwbsAssessmentEntityModel assessment)
    {
        return assessment.OptimismScore
            + assessment.UsefulnessScore
            + assessment.RelaxedScore
            + assessment.InterestedInPeopleScore
            + assessment.SpareEnergyScore
            + assessment.ProblemHandlingScore
            + assessment.ClearThoughtScore
            + assessment.FeelingGoodSelfScore
            + assessment.FeelingCloseToPeopleScore
            + assessment.ConfidenceScore
            + assessment.MakingUpOwnMindScore
            + assessment.FeelingLovedScore
            + assessment.InterestedInNewThingsScore
            + assessment.FeelingCheerfulScore;
    }

    private static IQueryable<WemwbsAssessmentEntityModel> ApplySorting(
        IQueryable<WemwbsAssessmentEntityModel> query,
        string? sortBy,
        string? sortOrder)
    {
        var descending = sortOrder?.ToLower() == "desc";

        return sortBy?.ToLower() switch
        {
            "createdat" => descending
                ? query.OrderByDescending(w => w.CreatedAt)
                : query.OrderBy(w => w.CreatedAt),
            "totalscore" => descending
                ? query.OrderByDescending(w => w.OptimismScore
                                                    + w.UsefulnessScore
                                                    + w.RelaxedScore
                                                    + w.InterestedInPeopleScore
                                                    + w.SpareEnergyScore
                                                    + w.ProblemHandlingScore
                                                    + w.ClearThoughtScore
                                                    + w.FeelingGoodSelfScore
                                                    + w.FeelingCloseToPeopleScore
                                                    + w.ConfidenceScore
                                                    + w.MakingUpOwnMindScore
                                                    + w.FeelingLovedScore
                                                    + w.InterestedInNewThingsScore
                                                    + w.FeelingCheerfulScore
                )
                : query.OrderBy(w => w.OptimismScore
                                        + w.UsefulnessScore
                                        + w.RelaxedScore
                                        + w.InterestedInPeopleScore
                                        + w.SpareEnergyScore
                                        + w.ProblemHandlingScore
                                        + w.ClearThoughtScore
                                        + w.FeelingGoodSelfScore
                                        + w.FeelingCloseToPeopleScore
                                        + w.ConfidenceScore
                                        + w.MakingUpOwnMindScore
                                        + w.FeelingLovedScore
                                        + w.InterestedInNewThingsScore
                                        + w.FeelingCheerfulScore
                ),
            _ => query.OrderByDescending(w => w.CreatedAt)
        };
    }
}
