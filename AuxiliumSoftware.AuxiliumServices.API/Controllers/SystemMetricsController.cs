using AuxiliumSoftware.AuxiliumServices.API.Common.ControllerBases;
using AuxiliumSoftware.AuxiliumServices.API.Models;
using AuxiliumSoftware.AuxiliumServices.API.Models.SystemMetric;
using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework;
using AuxiliumSoftware.AuxiliumServices.Common.Enumerators;
using AuxiliumSoftware.AuxiliumServices.Common.Metrics.Enumerators;
using AuxiliumSoftware.AuxiliumServices.Common.Metrics.Interfaces;
using AuxiliumSoftware.AuxiliumServices.Common.Metrics.Naming;
using AuxiliumSoftware.AuxiliumServices.Common.Metrics.Sinks;
using AuxiliumSoftware.AuxiliumServices.Common.Services;
using Microsoft.AspNetCore.Mvc;

namespace AuxiliumSoftware.AuxiliumServices.API.Controllers
{
    [ApiController]
    [Route("/api/v3/system-metrics")]
    [Tags("System Metrics")]
    public class SystemMetricsController : LoggedInControllerBase
    {
        private readonly IMetricSink _metricSink;

        public SystemMetricsController(
            ISystemSettingsService systemSettingsService,
            IConfiguration configuration,
            AuxiliumDbContext db,
            IWebApplicationFirewallService waf,
            ILogger<SystemMetricsController> logger,
            ITotpService totpService,
            IMetricSink metricSink
        )
        : base(systemSettingsService, configuration, db, waf, logger, totpService)
        {
            _metricSink = metricSink;
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
                return StatusCode(StatusCodes.Status404NotFound, new FailureResponseModel
                {
                    Detail = $"The metric key '{metricKey}' is not valid."
                });
            }

            var records = await _metricSink.ReadLatestAsync(key, 100, HttpContext.RequestAborted);

            return StatusCode(StatusCodes.Status200OK, new MetricsOverTimeResponseModel
            {
                MetricKey = key,
                Metrics = records.Select(r => new MetricEntryResponseModel
                {
                    CreatedAt = r.TimestampUtc,
                    MetricValue = r.Value,
                }).ToList()
            });
        }

        [HttpGet("latest")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> GetLatestSnapshot()
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            var adminError = await this.RequireAdminAsync();
            if (adminError != null) return adminError;

            var latest = await _metricSink.ReadLatestPerKeyAsync(HttpContext.RequestAborted);

            return StatusCode(StatusCodes.Status200OK, latest.Select(r => new
            {
                metricKey = JsonEnumNames<SystemMetricKeyEnum>.Name(r.Key),
                value = r.Value,
                label = r.Label,
                createdAt = r.TimestampUtc
            }));
        }
    }
}
