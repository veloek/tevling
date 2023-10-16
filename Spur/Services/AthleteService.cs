using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Spur.Clients;
using Spur.Data;
using Spur.Model;

namespace Spur.Services;

public class AthleteService : IAthleteService
{
    private readonly ILogger<AthleteService> _logger;
    private readonly IDataContext _dataContext;
    private readonly IStravaClient _stravaClient;
    private readonly AuthenticationStateProvider _authenticationStateProvider;

    public AthleteService(
        ILogger<AthleteService> logger,
        IDataContext dataContext,
        IStravaClient stravaClient,
        AuthenticationStateProvider authenticationStateProvider)
    {
        _logger = logger;
        _dataContext = dataContext;
        _stravaClient = stravaClient;
        _authenticationStateProvider = authenticationStateProvider;
    }

    public async Task<Athlete> GetAthleteByIdAsync(int athleteId, CancellationToken ct = default)
    {
        Athlete athlete = await _dataContext.Athletes
            .Include(a => a.Activities)
            .Include(a => a.Following)
            .Include(a => a.Followers)
            .FirstOrDefaultAsync(a => a.Id == athleteId, ct) ??
            throw new Exception("Unknown athlete id: " + athleteId);

        return athlete;
    }

    // TODO: Move this to some other (auth?) service
    public async Task<Athlete?> GetCurrentAthlete(CancellationToken ct = default)
    {
        AuthenticationState authenticationState = await _authenticationStateProvider
            .GetAuthenticationStateAsync();

        string athleteIdStr = authenticationState.User.FindFirst(
            ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

        Athlete? athlete = null;

        if (int.TryParse(athleteIdStr, out int athleteId))
        {
            athlete = await GetAthleteByIdAsync(athleteId, ct);
        }
        return athlete;
    }

    public async Task<Athlete> UpsertAthleteAsync(long stravaId, string name, string? imgUrl,
        string accessToken, string refreshToken, DateTimeOffset accessTokenExpiry,
        CancellationToken ct = default)
    {
        bool newAthlete = false;
        Athlete? athlete = await _dataContext.Athletes
            .FirstOrDefaultAsync(a => a.StravaId == stravaId, ct);

        if (athlete == null)
        {
            newAthlete = true;
            athlete = new Athlete { StravaId = stravaId, Created = DateTimeOffset.UtcNow };
        }

        athlete.Name = name;
        athlete.ImgUrl = imgUrl;
        athlete.AccessToken = accessToken;
        athlete.RefreshToken = refreshToken;
        athlete.AccessTokenExpiry = accessTokenExpiry;

        athlete = newAthlete
            ? await _dataContext.AddAthleteAsync(athlete, ct)
            : await _dataContext.UpdateAthleteAsync(athlete, ct);

        return athlete;
    }

    public async Task<string> GetAccessTokenAsync(int athleteId, CancellationToken ct = default)
    {
        Athlete athlete = await _dataContext.Athletes
            .FirstOrDefaultAsync(a => a.Id == athleteId, ct) ??
            throw new Exception("Unknown athlete id: " + athleteId);

        if ((athlete.AccessTokenExpiry - DateTimeOffset.Now) < TimeSpan.FromMinutes(1))
        {
            (string accessToken, DateTimeOffset expiry) = await RefreshAccessToken(athlete.RefreshToken, ct);
            athlete.AccessToken = accessToken;
            athlete.AccessTokenExpiry = expiry;
            athlete = await _dataContext.UpdateAthleteAsync(athlete, ct);
        }

        return athlete.AccessToken;
    }

    private async Task<(string accessToken, DateTimeOffset expiry)> RefreshAccessToken(
        string refreshToken, CancellationToken ct)
    {
        Strava.TokenResponse tokenResponse = await _stravaClient
            .GetAccessTokenByRefreshTokenAsync(refreshToken, ct);

        if (tokenResponse.AccessToken is null)
            throw new Exception("AccessToken is null");

        return (tokenResponse.AccessToken, DateTimeOffset.FromUnixTimeSeconds(tokenResponse.ExpiresAt));
    }
}
