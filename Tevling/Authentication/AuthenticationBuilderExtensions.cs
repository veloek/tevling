using Microsoft.AspNetCore.Authentication;

namespace Tevling.Authentication;

public static class AuthenticationBuilderExtensions
{
    public static AuthenticationBuilder AddStravaAuthentication(
        this AuthenticationBuilder builder,
        Action<StravaAuthenticationOptions>? configureOptions = null)
    {
        builder.AddScheme<StravaAuthenticationOptions, StravaAuthenticationHandler>(
            StravaAuthenticationDefaults.AuthenticationScheme,
            null,
            configureOptions);

        return builder;
    }
}
