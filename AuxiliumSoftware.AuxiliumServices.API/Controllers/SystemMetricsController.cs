using AuxiliumSoftware.AuxiliumServices.API.Common.ControllerBases;
using AuxiliumSoftware.AuxiliumServices.API.Models.SystemMetric;
using AuxiliumSoftware.AuxiliumServices.API.Models.Waf.ResponseModels;
using AuxiliumSoftware.AuxiliumServices.API.Models.Waf.ResponseModels.SubResponseModels;
using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework;
using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework.Enumerators;
using AuxiliumSoftware.AuxiliumServices.Common.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;

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

        [HttpGet("database-size")]
        [ProducesResponseType(typeof(MetricEntryResponseModel), StatusCodes.Status200OK)]
        public async Task<ActionResult<MetricEntryResponseModel>> GetDatabaseSizeOverTime()
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;
            var adminError = await this.RequireAdminAsync();
            if (adminError != null) return adminError;

            return StatusCode(StatusCodes.Status200OK, new MetricsOverTimeResponseModel
            {
                MetricKey = SystemMetricKeyEnum.Db_SizeBytes,
                Metrics = await this.Db.System_Metrics
                    .Where(m => m.MetricKey == SystemMetricKeyEnum.Db_SizeBytes)
                    .OrderByDescending(m => m.CreatedAtUtc)
                    .Take(100)
                    .Select(m => new MetricEntryResponseModel
                    {
                        Id = m.Id,
                        CreatedAt = m.CreatedAtUtc,
                        MetricValue = m.MetricValue
                    })
                    .ToListAsync()
            });
        }

        [HttpGet("lfs-size")]
        [ProducesResponseType(typeof(MetricsOverTimeResponseModel), StatusCodes.Status200OK)]
        public async Task<ActionResult<MetricsOverTimeResponseModel>> GetLfsSizeOverTime()
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;
            var adminError = await this.RequireAdminAsync();
            if (adminError != null) return adminError;

            return StatusCode(StatusCodes.Status200OK, new MetricsOverTimeResponseModel
            {
                MetricKey = SystemMetricKeyEnum.Lfs_SizeBytes,
                Metrics = await this.Db.System_Metrics
                    .Where(m => m.MetricKey == SystemMetricKeyEnum.Lfs_SizeBytes)
                    .OrderByDescending(m => m.CreatedAtUtc)
                    .Take(100)
                    .Select(m => new MetricEntryResponseModel
                    {
                        Id = m.Id,
                        CreatedAt = m.CreatedAtUtc,
                        MetricValue = m.MetricValue
                    })
                    .ToListAsync()
            });
        }

        [HttpGet("api-cpu-usage")]
        [ProducesResponseType(typeof(MetricsOverTimeResponseModel), StatusCodes.Status200OK)]
        public async Task<ActionResult<MetricsOverTimeResponseModel>> GetApiCpuUsageOverTime()
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;
            var adminError = await this.RequireAdminAsync();
            if (adminError != null) return adminError;

            return StatusCode(StatusCodes.Status200OK, new MetricsOverTimeResponseModel
            {
                MetricKey = SystemMetricKeyEnum.Api_CpuUsagePercentage,
                Metrics = await this.Db.System_Metrics
                    .Where(m => m.MetricKey == SystemMetricKeyEnum.Api_CpuUsagePercentage)
                    .OrderByDescending(m => m.CreatedAtUtc)
                    .Take(100)
                    .Select(m => new MetricEntryResponseModel
                    {
                        Id = m.Id,
                        CreatedAt = m.CreatedAtUtc,
                        MetricValue = m.MetricValue
                    })
                    .ToListAsync()
            });
        }

        [HttpGet("api-ram-usage")]
        [ProducesResponseType(typeof(MetricsOverTimeResponseModel), StatusCodes.Status200OK)]
        public async Task<ActionResult<MetricsOverTimeResponseModel>> GetApiRamUsageOverTime()
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;
            var adminError = await this.RequireAdminAsync();
            if (adminError != null) return adminError;

            return StatusCode(StatusCodes.Status200OK, new MetricsOverTimeResponseModel
            {
                MetricKey = SystemMetricKeyEnum.Api_MemoryUsageBytes,
                Metrics = await this.Db.System_Metrics
                    .Where(m => m.MetricKey == SystemMetricKeyEnum.Api_MemoryUsageBytes)
                    .OrderByDescending(m => m.CreatedAtUtc)
                    .Take(100)
                    .Select(m => new MetricEntryResponseModel
                    {
                        Id = m.Id,
                        CreatedAt = m.CreatedAtUtc,
                        MetricValue = m.MetricValue
                    })
                    .ToListAsync()
            });
        }

        [HttpGet("task-runner-cpu-usage")]
        [ProducesResponseType(typeof(MetricsOverTimeResponseModel), StatusCodes.Status200OK)]
        public async Task<ActionResult<MetricsOverTimeResponseModel>> GetTaskRunnerCpuUsageOverTime()
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;
            var adminError = await this.RequireAdminAsync();
            if (adminError != null) return adminError;

            return StatusCode(StatusCodes.Status200OK, new MetricsOverTimeResponseModel
            {
                MetricKey = SystemMetricKeyEnum.TaskRunner_CpuUsagePercentage,
                Metrics = await this.Db.System_Metrics
                    .Where(m => m.MetricKey == SystemMetricKeyEnum.TaskRunner_CpuUsagePercentage)
                    .OrderByDescending(m => m.CreatedAtUtc)
                    .Take(100)
                    .Select(m => new MetricEntryResponseModel
                    {
                        Id = m.Id,
                        CreatedAt = m.CreatedAtUtc,
                        MetricValue = m.MetricValue
                    })
                    .ToListAsync()
            });
        }

        [HttpGet("task-runner-ram-usage")]
        [ProducesResponseType(typeof(MetricsOverTimeResponseModel), StatusCodes.Status200OK)]
        public async Task<ActionResult<MetricsOverTimeResponseModel>> GetTaskRunnerRamUsageOverTime()
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;
            var adminError = await this.RequireAdminAsync();
            if (adminError != null) return adminError;

            return StatusCode(StatusCodes.Status200OK, new MetricsOverTimeResponseModel
            {
                MetricKey = SystemMetricKeyEnum.TaskRunner_MemoryUsageBytes,
                Metrics = await this.Db.System_Metrics
                    .Where(m => m.MetricKey == SystemMetricKeyEnum.TaskRunner_MemoryUsageBytes)
                    .OrderByDescending(m => m.CreatedAtUtc)
                    .Take(100)
                    .Select(m => new MetricEntryResponseModel
                    {
                        Id = m.Id,
                        CreatedAt = m.CreatedAtUtc,
                        MetricValue = m.MetricValue
                    })
                    .ToListAsync()
            });
        }
    }
}
