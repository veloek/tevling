namespace Tevling.RateLimiting;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNotFoundRateLimit(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<NotFoundRateLimitOptions>(
            configuration.GetSection(nameof(NotFoundRateLimitOptions)));
        services.AddDistributedMemoryCache(); // NB: Use another distributed cache provider if we need to run multiple instances
        services.AddSingleton<NotFoundRateLimitTracker>();
        services.AddTransient<NotFoundRateLimitMiddleware>();
        return services;
    }
}
