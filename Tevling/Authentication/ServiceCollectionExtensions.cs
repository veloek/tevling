namespace Tevling.Authentication;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTokenRefresh(this IServiceCollection services)
    {
        services.AddTransient<TokenRefreshMiddleware>();
        return services;
    }
}
