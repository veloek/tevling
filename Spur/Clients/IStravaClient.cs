using Spur.Strava;

namespace Spur.Clients;

public interface IStravaClient
{
    Task<TokenResponse> GetAccessTokenByAuthorizationCodeAsync(
        string authorizationCode, CancellationToken ct = default);

    Task<TokenResponse> GetAccessTokenByRefreshTokenAsync(
        string refreshToken, CancellationToken ct = default);

    Task<Activity> GetActivityAsync(long stravaId, string accessToken,
        CancellationToken ct = default);

    Task<Activity[]> GetAthleteActivitiesAsync(
        string accessToken,
        DateTimeOffset? before = null,
        DateTimeOffset? after = null,
        int? page = null,
        int? pageSize = null,
        CancellationToken ct = default);
}
