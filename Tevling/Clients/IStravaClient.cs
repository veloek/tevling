using Tevling.Strava;

namespace Tevling.Clients;

public interface IStravaClient
{
    Task<TokenResponse> GetAccessTokenByAuthorizationCodeAsync(
        string authorizationCode, CancellationToken ct = default);

    Task<TokenResponse> GetAccessTokenByRefreshTokenAsync(
        string refreshToken, CancellationToken ct = default);

    Task<DetailedActivity> GetActivityAsync(long stravaId, string accessToken,
        CancellationToken ct = default);

    Task<SummaryActivity[]> GetAthleteActivitiesAsync(
        string accessToken,
        DateTimeOffset? before = null,
        DateTimeOffset? after = null,
        int? page = null,
        int? pageSize = null,
        CancellationToken ct = default);

    Task DeauthorizeAppAsync(string accessToken, CancellationToken ct = default);
}
