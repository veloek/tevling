using Spur.Model;

namespace Spur.Data;
public interface IActivityRepository
{
    Task<Activity?> GetActivityAsync(int athleteId, long stravaId,
        CancellationToken ct = default);
    IAsyncEnumerable<Activity> GetActivitiesForAthlete(int athleteId,
        CancellationToken ct = default);
    Task<Activity> AddActivityAsync(int athleteId, long stravaId,
        CancellationToken ct = default);
    Task<Activity> UpdateActivityAsync(Activity activity,
        CancellationToken ct = default);
    Task<Activity> RemoveActivityAsync(Activity activity,
        CancellationToken ct = default);
}
