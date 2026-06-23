using AuxiliumSoftware.AuxiliumServices.API.Common.ControllerBases;
using AuxiliumSoftware.AuxiliumServices.API.Models;
using AuxiliumSoftware.AuxiliumServices.API.Models.SystemMetric;
using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework;
using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework.Enumerators;
using AuxiliumSoftware.AuxiliumServices.Common.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuxiliumSoftware.AuxiliumServices.API.Controllers
{
    [ApiController]
    [Route("/api/v3/system-metrics")]
    [Tags("System Metrics")]
    public class SystemMetricsController : LoggedInControllerBase
    {
        public SystemMetricsController(
            ISystemSettingsService systemSettingsService,
            IConfiguration configuration,
            AuxiliumDbContext db,
            IWebApplicationFirewallService waf,
            ILogger<SystemMetricsController> logger,
            ITotpService totpService
        )
        : base(systemSettingsService, configuration, db, waf, logger, totpService)
        {
        }

        [HttpGet("series/{metricKey}")]
        [ProducesResponseType(typeof(MetricsOverTimeResponseModel), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<MetricsOverTimeResponseModel>> GetSeries(string metricKey)
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            var adminError = await this.RequireAdminAsync();
            if (adminError != null) return adminError;

            if (!Enum.TryParse<SystemMetricKeyEnum>(metricKey, ignoreCase: true, out var key) || !Enum.IsDefined(key))
            {
                return StatusCode(
                    StatusCodes.Status404NotFound,
                    new FailureResponseModel
                    {
                        Detail = $"The metric key '{metricKey}' is not valid."
                    }
                );
            }

            return StatusCode(StatusCodes.Status200OK, await BuildSeriesAsync(key));
        }

        [HttpGet("latest")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> GetLatestSnapshot()
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            var adminError = await this.RequireAdminAsync();
            if (adminError != null) return adminError;

            var latest = await this.Db.System_Metrics
                .GroupBy(m => m.MetricKey)
                .Select(g => g.OrderByDescending(m => m.CreatedAtUtc).First())
                .ToListAsync();

            return StatusCode(StatusCodes.Status200OK, latest.Select(m => new
            {
                metricKey = m.MetricKey.ToString(),
                value = m.MetricValue,
                createdAt = m.CreatedAtUtc
            }));
        }

        private async Task<MetricsOverTimeResponseModel> BuildSeriesAsync(SystemMetricKeyEnum key)
        {
            return new MetricsOverTimeResponseModel
            {
                MetricKey = key,
                Metrics = await this.Db.System_Metrics
                    .Where(m => m.MetricKey == key)
                    .OrderByDescending(m => m.CreatedAtUtc)
                    .Take(100)
                    .Select(m => new MetricEntryResponseModel
                    {
                        Id = m.Id,
                        CreatedAt = m.CreatedAtUtc,
                        MetricValue = m.MetricValue
                    })
                    .ToListAsync()
            };
        }
    }
}
