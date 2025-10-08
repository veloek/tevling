using System.Globalization;
using Microsoft.Extensions.Options;

namespace Tevling;

public class CultureByHost() : Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { }

public class CultureByHostMiddleware(IOptions<CultureByHost> cultureByHost, RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        string host = context.Request.Host.Host;
        string? culture = cultureByHost.Value.GetValueOrDefault(host);

        if (culture is not null)
        {
            CultureInfo cultureInfo = new(culture);

            CultureInfo.CurrentCulture = cultureInfo;
            CultureInfo.CurrentUICulture = cultureInfo;
        }

        await next(context);
    }
}

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseCultureByHost(this IApplicationBuilder builder)
        => builder.UseMiddleware<CultureByHostMiddleware>();
}
