using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;

namespace Tevling.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly ILogger<AuthenticationService> _logger;
    private readonly AuthenticationStateProvider _authenticationStateProvider;
    private readonly IAthleteService _athleteService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IStravaClient _stravaClient;

    public AuthenticationService(
        ILogger<AuthenticationService> logger,
        AuthenticationStateProvider authenticationStateProvider,
        IAthleteService athleteService,
        IHttpContextAccessor httpContextAccessor,
        IStravaClient stravaClient)
    {
        _logger = logger;
        _authenticationStateProvider = authenticationStateProvider;
        _athleteService = athleteService;
        _httpContextAccessor = httpContextAccessor;
        _stravaClient = stravaClient;
    }

    public async Task LoginAsync(Athlete athlete, CancellationToken ct = default)
    {
        List<Claim> claims = new()
        {
            new Claim(ClaimTypes.Name, athlete.Name),
            new Claim(ClaimTypes.NameIdentifier, athlete.Id.ToString()),
        };

        ClaimsIdentity claimsIdentity = new(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme);

        AuthenticationProperties authProperties = new()
        {
            AllowRefresh = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(1),
            IsPersistent = true,
            RedirectUri = "/",
        };

        HttpContext httpContext = _httpContextAccessor.HttpContext ??
            throw new InvalidOperationException("No active HttpContext");

        await httpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);

        _logger.LogInformation("User ID {AthleteId} logged in at {DateTime}", athlete.Id, DateTime.Now);
    }

    public async Task<Athlete> GetCurrentAthleteAsync(CancellationToken ct = default)
    {
        AuthenticationState authenticationState = await _authenticationStateProvider
            .GetAuthenticationStateAsync();

        string athleteIdStr = authenticationState.User.FindFirst(
                    ClaimTypes.NameIdentifier)
                ?.Value ??
            string.Empty;

        Athlete? athlete = null;

        if (int.TryParse(athleteIdStr, out int athleteId))
        {
            athlete = await _athleteService.GetAthleteByIdAsync(athleteId, ct);
        }

        if (athlete is null)
        {
            throw new Exception("Logged in athlete not found");
        }

        return athlete;
    }

    public async Task LogoutAsync(bool deauthorizeApp = false, CancellationToken ct = default)
    {
        HttpContext httpContext = _httpContextAccessor.HttpContext ??
            throw new InvalidOperationException("No active HttpContext");

        string? athleteIdStr = httpContext.User.Claims
            .FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)
            ?.Value;

        int athleteId = int.TryParse(athleteIdStr, out int parsed) ? parsed : default;

        if (deauthorizeApp)
        {
            string accessToken = await _athleteService.GetAccessTokenAsync(athleteId, ct);
            await _stravaClient.DeauthorizeAppAsync(accessToken, ct);
        }

        await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        _logger.LogInformation("User ID {AthleteId} logged out at {DateTime}", athleteId, DateTime.Now);
    }
}
