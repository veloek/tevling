using Spur.Model;

namespace Spur.Data;
public interface IAthleteRepository
{
    Task<bool> AthleteExistsAsync(long stravaId, CancellationToken ct = default);
    Task<Athlete> CreateAthleteAsync(long stravaId, string name, string? imgUrl,
        CancellationToken ct = default);
}
