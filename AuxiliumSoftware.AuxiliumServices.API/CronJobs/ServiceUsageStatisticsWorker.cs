using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework;
using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework.EntityModels;
using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework.Enumerators;
using AuxiliumSoftware.AuxiliumServices.Common.Enumerators;
using AuxiliumSoftware.AuxiliumServices.Common.Superclasses;
using AuxiliumSoftware.AuxiliumServices.Common.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace AuxiliumSoftware.AuxiliumServices.API.CronJobs
{
    public class ServiceUsageStatisticsWorker : CronBackgroundService
    {
        private const string Schedule = "0 * * * *"; // every hour on the hour

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ServiceUsageStatisticsWorker> _logger;

        public ServiceUsageStatisticsWorker(IServiceScopeFactory scopeFactory, ILogger<ServiceUsageStatisticsWorker> logger)
            : base(Schedule, TimeZoneInfo.Utc, logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task DoWorkAsync(CancellationToken stoppingToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AuxiliumDbContext>();

            float cpuUsage = await GetProcessCpuUsageAsync(stoppingToken);
            float memsize = Environment.WorkingSet;

            db.System_Metrics.Add(new SystemMetricEntityModel
            {
                Id = UUIDUtilities.GenerateV5(DatabaseObjectTypeEnum.System_MetricEntry),
                CreatedAtUtc = DateTime.UtcNow,
                MetricKey = SystemMetricKeyEnum.Api_MemoryUsageBytes,
                MetricValue = memsize,
            });
            db.System_Metrics.Add(new SystemMetricEntityModel
            {
                Id = UUIDUtilities.GenerateV5(DatabaseObjectTypeEnum.System_MetricEntry),
                CreatedAtUtc = DateTime.UtcNow,
                MetricKey = SystemMetricKeyEnum.Api_CpuUsagePercentage,
                MetricValue = cpuUsage,
            });

            await db.SaveChangesAsync(stoppingToken);
        }

        private static async Task<float> GetProcessCpuUsageAsync(CancellationToken stoppingToken)
        {
            const int sampleMs = 500;

            using Process proc = Process.GetCurrentProcess();
            TimeSpan startCpu = proc.TotalProcessorTime;
            Stopwatch sw = Stopwatch.StartNew();

            await Task.Delay(sampleMs, stoppingToken);

            sw.Stop();
            proc.Refresh();
            double cpuUsedMs = (proc.TotalProcessorTime - startCpu).TotalMilliseconds;

            double usage = cpuUsedMs / (Environment.ProcessorCount * sw.Elapsed.TotalMilliseconds) * 100d;

            return (float)Math.Clamp(usage, 0d, 100d);
        }
    }
}
