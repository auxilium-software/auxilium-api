using AuxiliumSoftware.AuxiliumServices.API.Metrics;
using System.Diagnostics;

namespace AuxiliumSoftware.AuxiliumServices.API.Middleware
{
    public sealed class HttpMetricsMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly HttpMetricsAccumulator _acc;

        public HttpMetricsMiddleware(RequestDelegate next, HttpMetricsAccumulator acc)
        {
            _next = next;
            _acc = acc;
        }

        public async Task Invoke(HttpContext ctx)
        {
            var sw = Stopwatch.StartNew();
            var threw = false;
            try
            {
                await _next(ctx);
            }
            catch
            {
                threw = true;
                throw;
            }
            finally
            {
                sw.Stop();
                _acc.Record(sw.Elapsed.TotalMilliseconds, ctx.Response.StatusCode, threw);
            }
        }
    }
}
