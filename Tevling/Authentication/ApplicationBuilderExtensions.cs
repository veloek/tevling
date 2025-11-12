namespace Tevling.Authentication;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseTokenRefresh(this IApplicationBuilder builder)
    {
        builder.UseMiddleware<TokenRefreshMiddleware>();
        return builder;
    }
}
