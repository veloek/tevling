namespace Spur.Services;

public interface IAthleteService
{
    Task<string> GetAccessTokenAsync(int athleteId, CancellationToken ct = default);
}
