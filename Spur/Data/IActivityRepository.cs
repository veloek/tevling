using Spur.Model;

namespace Spur.Data;
public interface IActivityRepository
{
    Task<Activity> AddActivityAsync(int athleteId, long stravaId,
        CancellationToken ct = default);
}
