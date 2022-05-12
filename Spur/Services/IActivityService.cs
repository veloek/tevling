using Spur.Model;

namespace Spur.Services;

public interface IActivityService
{
    Task<Activity> CreateActivityAsync(long stravaAthleteId, long stravaActivityId,
        CancellationToken ct = default);

    Task<Activity> UpdateActivityAsync(long stravaAthleteId, long stravaActivityId,
        CancellationToken ct = default);

    Task DeleteActivityAsync(long stravaAthleteId, long stravaActivityId,
        CancellationToken ct = default);
}
