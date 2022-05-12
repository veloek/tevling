using Spur.Model;

namespace Spur.Services;

public interface IAthleteService
{
    Task<string> GetAccessTokenAsync(int athleteId, CancellationToken ct = default);
    Task<Athlete> GetAthleteByIdAsync(int athleteId, CancellationToken ct = default);
}
