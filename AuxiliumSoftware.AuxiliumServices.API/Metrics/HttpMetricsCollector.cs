using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework.Enumerators;
using AuxiliumSoftware.AuxiliumServices.Common.Enumerators;
using AuxiliumSoftware.AuxiliumServices.Common.Metrics.Common;
using AuxiliumSoftware.AuxiliumServices.Common.Metrics.Interfaces;

namespace AuxiliumSoftware.AuxiliumServices.API.Metrics
{
    public sealed class HttpMetricsCollector : IMetricCollector
    {
        private readonly HttpMetricsAccumulator _acc;

        public HttpMetricsCollector(HttpMetricsAccumulator acc) => _acc = acc;

        public MetricCadence Cadence => MetricCadence.Minutely;

        public Task<IReadOnlyList<MetricSample>> CollectAsync(CancellationToken ct)
        {
            var w = _acc.Drain();
            IReadOnlyList<MetricSample> samples = new[]
            {
                new MetricSample(SystemMetricKeyEnum.Api_RequestsPerMinute, w.Requests),
                new MetricSample(SystemMetricKeyEnum.Api_Http5xxPerMinute, w.Server5xx),
                new MetricSample(SystemMetricKeyEnum.Api_ExceptionsPerMinute, w.Exceptions),
                new MetricSample(SystemMetricKeyEnum.Api_ResponseTimeP50InMs, w.P50),
                new MetricSample(SystemMetricKeyEnum.Api_ResponseTimeP95InMs, w.P95),
                new MetricSample(SystemMetricKeyEnum.Api_ResponseTimeP99InMs, w.P99),
            };
            return Task.FromResult(samples);
        }
    }

}
