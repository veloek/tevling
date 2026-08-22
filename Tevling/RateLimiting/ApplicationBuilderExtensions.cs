namespace Tevling.RateLimiting;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseNotFoundRateLimit(this IApplicationBuilder builder)
    {
        builder.UseMiddleware<NotFoundRateLimitMiddleware>();
        return builder;
    }
}
