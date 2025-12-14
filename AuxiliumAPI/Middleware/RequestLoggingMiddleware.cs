using System.Diagnostics;
using System.Text;

namespace AuxiliumAPI.Middleware
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();
            var requestId = Guid.NewGuid().ToString("N")[..8];

            // log the start of the request
            _logger.LogInformation(
                "[{RequestId}] -> {Method} {Path}{QueryString} | IP: {IP}",
                requestId,
                context.Request.Method,
                context.Request.Path,
                context.Request.QueryString,
                context.Connection.RemoteIpAddress
            );

            // grab the response
            var originalBodyStream = context.Response.Body;

            try
            {
                await _next(context);

                stopwatch.Stop();

                // log response
                _logger.LogInformation(
                    "[{RequestId}] <- {StatusCode} {Method} {Path} | {ElapsedMs}ms",
                    requestId,
                    context.Response.StatusCode,
                    context.Request.Method,
                    context.Request.Path,
                    stopwatch.ElapsedMilliseconds
                );

                // warn log slow requests
                if (stopwatch.ElapsedMilliseconds > 1000)
                {
                    _logger.LogWarning(
                        "[{RequestId}] ⚠️ SLOW REQUEST ⚠️: {Method} {Path} took {ElapsedMs}ms",
                        requestId,
                        context.Request.Method,
                        context.Request.Path,
                        stopwatch.ElapsedMilliseconds
                    );
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                // error log exceptions
                _logger.LogError(ex,
                    "[{RequestId}] ❌ EXCEPTION ❌: {Method} {Path} after {ElapsedMs}ms",
                    requestId,
                    context.Request.Method,
                    context.Request.Path,
                    stopwatch.ElapsedMilliseconds
                );

                throw;
            }
            finally
            {
                context.Response.Body = originalBodyStream;
            }
        }
    }

}
