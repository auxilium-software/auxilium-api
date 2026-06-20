using System.Linq.Expressions;
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
    /// Search through all WEMWBS assessments.
    /// This is an Administrator-only endpoint.
    /// </summary>
    [HttpGet("")]
    [ProducesResponseType(typeof(PaginatedWEMWBSResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PaginatedWEMWBSResponseModel>> SearchAssessments(
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

            var adminError = await RequireAdminAsync();
            if (adminError != null) return adminError;

            var query = Db.UserWemwbsAssessments.AsQueryable();

            // apply sorting
            query = ApplySorting(query, sortBy, sortOrder);

            var total = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(total / (double)pageSize);

            var assessments = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var response = new PaginatedWEMWBSResponseModel
            {
                Assessments = assessments.Select(MapToResponse).ToList(),
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
            this.Logger.LogError(ex, "Failed to fetch WEMWBS assessments");
            return StatusCode(StatusCodes.Status500InternalServerError, new FailureResponseModel
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

            var assessment = await Db.UserWemwbsAssessments
                .FirstOrDefaultAsync(w => w.Id == assessmentId);

            if (assessment == null)
            {
                return StatusCode(StatusCodes.Status404NotFound, new FailureResponseModel
                {
                    Detail = "Assessment not found"
                });
            }

            // an assessment is owned by its SUBJECT, not its author.
            // only the subject (or an admin) may read it; 404 rather than 403 so we don't leak the existence of a row to someone it isn't about.
            if (assessment.UserId != user!.Id && !user.IsAdministrator)
            {
                return StatusCode(StatusCodes.Status404NotFound, new FailureResponseModel
                {
                    Detail = "Assessment not found"
                });
            }

            return StatusCode(StatusCodes.Status200OK, MapToResponse(assessment));
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Failed to fetch WEMWBS assessment {AssessmentId}", assessmentId);
            return StatusCode(StatusCodes.Status500InternalServerError, new FailureResponseModel
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
    [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<WEMWBSResponseModel>> CreateAssessment([FromBody] CreateWEMWBSRequestModel request)
    {
        try
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            var scores = new[]
            {
                request.Responses.OptimismScore,
                request.Responses.UsefulnessScore,
                request.Responses.RelaxedScore,
                request.Responses.InterestedInPeopleScore,
                request.Responses.SpareEnergyScore,
                request.Responses.ProblemHandlingScore,
                request.Responses.ClearThoughtScore,
                request.Responses.FeelingGoodSelfScore,
                request.Responses.FeelingCloseToPeopleScore,
                request.Responses.ConfidenceScore,
                request.Responses.MakingUpOwnMindScore,
                request.Responses.FeelingLovedScore,
                request.Responses.InterestedInNewThingsScore,
                request.Responses.FeelingCheerfulScore
            };

            if (scores.Any(s => s is < 1 or > 5))
            {
                return StatusCode(StatusCodes.Status400BadRequest, new FailureResponseModel
                {
                    Detail = "All scores must be between 1 and 5"
                });
            }

            if (request.SubjectUserId == Guid.Empty)
            {
                return StatusCode(StatusCodes.Status400BadRequest, new FailureResponseModel
                {
                    Detail = "A subject user must be specified"
                });
            }

            // self-submission is always fine; naming a different subject is privileged
            if (request.SubjectUserId != user!.Id)
            {
                if (!user.IsAdministrator)
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new FailureResponseModel
                    {
                        Detail = "You cannot create an assessment for another user"
                    });
                }

                var subjectExists = await Db.Users.AnyAsync(u => u.Id == request.SubjectUserId);
                if (!subjectExists)
                {
                    return StatusCode(StatusCodes.Status400BadRequest, new FailureResponseModel
                    {
                        Detail = "Subject user does not exist"
                    });
                }
            }

            var assessment = new WemwbsAssessmentEntityModel
            {
                Id = Guid.NewGuid(),
                CreatedAtUtc = DateTime.UtcNow,
                CreatedByUserId = user!.Id,
                UserId = request.SubjectUserId,
                OptimismScore = request.Responses.OptimismScore,
                UsefulnessScore = request.Responses.UsefulnessScore,
                RelaxedScore = request.Responses.RelaxedScore,
                InterestedInPeopleScore = request.Responses.InterestedInPeopleScore,
                SpareEnergyScore = request.Responses.SpareEnergyScore,
                ProblemHandlingScore = request.Responses.ProblemHandlingScore,
                ClearThoughtScore = request.Responses.ClearThoughtScore,
                FeelingGoodSelfScore = request.Responses.FeelingGoodSelfScore,
                FeelingCloseToPeopleScore = request.Responses.FeelingCloseToPeopleScore,
                ConfidenceScore = request.Responses.ConfidenceScore,
                MakingUpOwnMindScore = request.Responses.MakingUpOwnMindScore,
                FeelingLovedScore = request.Responses.FeelingLovedScore,
                InterestedInNewThingsScore = request.Responses.InterestedInNewThingsScore,
                FeelingCheerfulScore = request.Responses.FeelingCheerfulScore
            };

            Db.UserWemwbsAssessments.Add(assessment);
            await Db.SaveChangesAsync();

            return StatusCode(StatusCodes.Status201Created, MapToResponse(assessment));
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Failed to create WEMWBS assessment");
            return StatusCode(StatusCodes.Status500InternalServerError, new FailureResponseModel
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
            var (_, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            var adminError = await RequireAdminAsync();
            if (adminError != null) return adminError;

            // assessments ABOUT the target user (subject), regardless of who authored them
            var query = Db.UserWemwbsAssessments
                .Where(w => w.UserId == targetUserId);

            // apply sorting
            query = ApplySorting(query, sortBy, sortOrder);

            var total = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(total / (double)pageSize);

            var assessments = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var response = new PaginatedWEMWBSResponseModel
            {
                Assessments = assessments.Select(MapToResponse).ToList(),
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
            this.Logger.LogError(ex, "Failed to fetch user WEMWBS assessments for user {UserId}", targetUserId);
            return StatusCode(StatusCodes.Status500InternalServerError, new FailureResponseModel
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
            var (_, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            var adminError = await RequireAdminAsync();
            if (adminError != null) return adminError;

            var assessment = await Db.UserWemwbsAssessments.FindAsync(assessmentId);

            if (assessment == null)
            {
                return StatusCode(StatusCodes.Status404NotFound, new FailureResponseModel
                {
                    Detail = "Assessment not found"
                });
            }

            Db.UserWemwbsAssessments.Remove(assessment);
            await Db.SaveChangesAsync();

            return StatusCode(StatusCodes.Status204NoContent);
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Failed to delete WEMWBS assessment {AssessmentId}", assessmentId);
            return StatusCode(StatusCodes.Status500InternalServerError, new FailureResponseModel
            {
                Detail = "Failed to delete WEMWBS assessment"
            });
        }
    }

    /// <summary>
    /// Get statistics for the authenticated user
    /// </summary>
    [HttpGet("my-statistics")]
    [ProducesResponseType(typeof(WEMWBSStatisticsResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<WEMWBSStatisticsResponseModel>> GetMyStatistics()
    {
        try
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            // statistics over assessments ABOUT me (subject), not ones I authored
            var assessments = await Db.UserWemwbsAssessments
                .Where(w => w.UserId == user!.Id)
                .OrderBy(w => w.CreatedAtUtc)
                .ToListAsync();

            if (!assessments.Any())
            {
                return StatusCode(StatusCodes.Status200OK, new WEMWBSStatisticsResponseModel
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

            return StatusCode(StatusCodes.Status200OK, new WEMWBSStatisticsResponseModel
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
            return StatusCode(StatusCodes.Status500InternalServerError, new FailureResponseModel
            {
                Detail = "Failed to fetch WEMWBS statistics"
            });
        }
    }





    private static readonly Expression<Func<WemwbsAssessmentEntityModel, int>> TotalScoreExpression =
        w => w.OptimismScore
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
           + w.FeelingCheerfulScore;

    private static readonly Func<WemwbsAssessmentEntityModel, int> CalculateTotalScore =
        TotalScoreExpression.Compile();

    private static WEMWBSResponseModel MapToResponse(WemwbsAssessmentEntityModel a) => new()
    {
        Id = a.Id,
        CreatedAt = a.CreatedAtUtc,
        CreatedBy = a.CreatedByUserId,
        Subject = a.UserId,
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
    };

    private static IQueryable<WemwbsAssessmentEntityModel> ApplySorting(
        IQueryable<WemwbsAssessmentEntityModel> query,
        string? sortBy,
        string? sortOrder)
    {
        var descending = sortOrder?.ToLower() == "desc";

        return sortBy?.ToLower() switch
        {
            "createdat" => descending
                ? query.OrderByDescending(w => w.CreatedAtUtc)
                : query.OrderBy(w => w.CreatedAtUtc),
            "totalscore" => descending
                ? query.OrderByDescending(TotalScoreExpression)
                : query.OrderBy(TotalScoreExpression),
            _ => query.OrderByDescending(w => w.CreatedAtUtc)
        };
    }
}
