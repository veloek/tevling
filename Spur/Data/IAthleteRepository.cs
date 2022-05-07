using Spur.Model;

namespace Spur.Data;
public interface IAthleteRepository
{
    Task<bool> AthleteExistsAsync(long stravaId, CancellationToken ct = default);
    Task<Athlete> UpsertAthleteAsync(long stravaId, string name, string? imgUrl,
        string accessToken, string refreshToken, DateTimeOffset accessTokenExpiry,
        CancellationToken ct = default);
    Task<Athlete?> GetAthleteByStravaIdAsync(long stravaId, CancellationToken ct = default);
    Task<Athlete?> GetAthleteByIdAsync(long athleteId, CancellationToken ct = default);
}
