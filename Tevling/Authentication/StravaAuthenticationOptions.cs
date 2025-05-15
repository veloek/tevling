using Microsoft.AspNetCore.Authentication;

namespace Tevling.Authentication;

public class StravaAuthenticationOptions : AuthenticationSchemeOptions
{
    public string TokenHeaderName { get; set; } = "X-Strava-Token";
    public string TokenQueryName { get; set; } = "token";
}
