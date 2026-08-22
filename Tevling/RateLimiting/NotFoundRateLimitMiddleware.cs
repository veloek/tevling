using Microsoft.Extensions.Options;

namespace Tevling.RateLimiting;

public class NotFoundRateLimitMiddleware(
    NotFoundRateLimitTracker tracker,
    IOptions<NotFoundRateLimitOptions> options,
    ILogger<NotFoundRateLimitMiddleware> logger)
    : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        // NB: If the app lies behind a proxy, the IP address will be that of the proxy.
        // To solve this, the proxy must be configured to forward the client IP
        // and this app must make use of the ForwardedHeaders middleware.
        string clientKey = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        if (await tracker.IsExceededAsync(clientKey, context.RequestAborted))
        {
            logger.LogWarning(
                "\"Not-found\" rate limit exceeded for {Client} on {Path}",
                clientKey,
                context.Request.Path);

            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.Headers.RetryAfter =
                ((int)options.Value.Window.TotalSeconds).ToString();
            return;
        }

        context.Response.OnStarting(async () =>
        {
            if (context.Response.StatusCode == StatusCodes.Status404NotFound)
                await tracker.RegisterNotFoundAsync(clientKey, context.RequestAborted);
        });

        await next(context);
    }
}
