using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Spur.Model;

namespace Spur.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly ILogger<AuthenticationService> _logger;
    private readonly AuthenticationStateProvider _authenticationStateProvider;
    private readonly IAthleteService _athleteService;

    public AuthenticationService(
        ILogger<AuthenticationService> logger,
        AuthenticationStateProvider authenticationStateProvider,
        IAthleteService athleteService)
    {
        _logger = logger;
        _authenticationStateProvider = authenticationStateProvider;
        _athleteService = athleteService;
    }

    public async Task LoginAsync(HttpContext httpContext, Athlete athlete, CancellationToken ct = default)
    {
        List<Claim> claims = new()
        {
            new Claim(ClaimTypes.Name, athlete.Name),
            new Claim(ClaimTypes.NameIdentifier, athlete.Id.ToString()),
        };

        ClaimsIdentity claimsIdentity = new(
            claims, CookieAuthenticationDefaults.AuthenticationScheme);

        AuthenticationProperties authProperties = new()
        {
            AllowRefresh = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(1),
            IsPersistent = true,
            RedirectUri = "/",
        };

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
            ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

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
}
