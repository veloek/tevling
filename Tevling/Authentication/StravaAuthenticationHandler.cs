using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Tevling.Strava;

namespace Tevling.Authentication;

public class StravaAuthenticationHandler(
    IStravaClient stravaClient,
    IAthleteService athleteService,
    IOptionsMonitor<StravaAuthenticationOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<StravaAuthenticationOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(Options.TokenHeaderName, out StringValues tokenValues) &&
            !Request.Query.TryGetValue(Options.TokenQueryName, out tokenValues))
        {
            return AuthenticateResult.NoResult();
        }

        string? token = tokenValues.Single();

        if (token is null)
        {
            return AuthenticateResult.Fail("Token is null");
        }

        SummaryAthlete stravaAthlete = await stravaClient.GetAuthenticatedAthleteAsync(token);
        Athlete? athlete = await athleteService.GetAthleteByStravaIdAsync(stravaAthlete.Id);

        if (athlete is null)
        {
            return AuthenticateResult.Fail("Unknown athlete");
        }

        Claim[] claims =
        [
            new(ClaimTypes.Name, athlete.Name),
            new(ClaimTypes.NameIdentifier, athlete.Id.ToString()),
        ];

        ClaimsIdentity claimsIdentity = new(claims, Scheme.Name);
        ClaimsPrincipal claimsPrincipal = new(claimsIdentity);
        AuthenticationTicket authenticationTicket = new(claimsPrincipal, Scheme.Name);

        return AuthenticateResult.Success(authenticationTicket);
    }
}
