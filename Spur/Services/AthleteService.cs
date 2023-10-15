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

    public AthleteService(
        ILogger<AthleteService> logger,
        IDataContext dataContext,
        IStravaClient stravaClient)
    {
        _logger = logger;
        _dataContext = dataContext;
        _stravaClient = stravaClient;
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
            athlete.AccessToken = await RefreshAccessToken(athlete.RefreshToken, ct);
            athlete = await _dataContext.UpdateAthleteAsync(athlete, ct);
        }

        return athlete.AccessToken;
    }

    private async Task<string> RefreshAccessToken(string refreshToken, CancellationToken ct)
    {
        Strava.TokenResponse tokenResponse = await _stravaClient.GetAccessTokenByRefreshTokenAsync(refreshToken, ct);

        if (tokenResponse.AccessToken is null)
            throw new Exception("AccessToken is null");

        return tokenResponse.AccessToken;
    }
}
