using Spur.Model;

namespace Spur.Services;

public interface IAthleteService
{
    Task<Athlete> GetAthleteByIdAsync(int athleteId, CancellationToken ct = default);
    Task<Athlete> UpsertAthleteAsync(long stravaId, string name, string? imgUrl,
        string accessToken, string refreshToken, DateTimeOffset accessTokenExpiry,
        CancellationToken ct = default);
    Task<string> GetAccessTokenAsync(int athleteId, CancellationToken ct = default);
}
