using AuxiliumSoftware.AuxiliumServices.Common.Services;

namespace AuxiliumSoftware.AuxiliumServices.API.Middleware
{
    public class WafIpMiddleware
    {
        private readonly RequestDelegate _next;

        public WafIpMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IWebApplicationFirewallService waf)
        {
            var ipAddress = context.Connection.RemoteIpAddress;
            if (ipAddress == null)
            {
                await _next(context);
                return;
            }

            var block = await waf.IsIpAddressBlacklistedAsync(ipAddress, context.RequestAborted);
            if (block != null)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "ip_blocked",
                    message = "Access denied",
                    permanent = block.IsPermanent
                });
                return;
            }

            if (await waf.IsIpAddressRateLimitedAsync(ipAddress, context.RequestAborted))
            {
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.Response.Headers.RetryAfter = "60";
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "rate_limited",
                    message = "Too many requests"
                });
                return;
            }

            await _next(context);
        }
    }
}
