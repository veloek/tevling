using Spur.Clients;
using Spur.Data;

namespace Spur.Services;

public class AthleteService : IAthleteService
{
    private readonly ILogger<AthleteService> _logger;
    private readonly IAthleteRepository _athleteRepository;
    private readonly IDataContext _dataContext;
    private readonly IStravaClient _stravaClient;


    public AthleteService(
        ILogger<AthleteService> logger,
        IAthleteRepository athleteRepository,
        IDataContext dataContext,
        IStravaClient stravaClient)
    {
        _logger = logger;
        _athleteRepository = athleteRepository;
        _dataContext = dataContext;
        _stravaClient = stravaClient;
    }

    public async Task<string> GetAccessTokenAsync(int athleteId, CancellationToken ct = default)
    {
        var athlete = await _athleteRepository.GetAthleteByIdAsync(athleteId, ct);
        if (athlete == null)
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
        var tokenResponse = await _stravaClient.GetAccessTokenByRefreshTokenAsync(refreshToken, ct);

        if (tokenResponse.AccessToken is null)
            throw new Exception("AccessToken is null");

        return tokenResponse.AccessToken;
    }
}
