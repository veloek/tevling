namespace Tevling.Services;

public interface IActivityService
{
    Task<Activity> CreateActivityAsync(
        long stravaAthleteId,
        long stravaActivityId,
        CancellationToken ct = default);

    Task<Activity> UpdateActivityAsync(
        long stravaAthleteId,
        long stravaActivityId,
        CancellationToken ct = default);

    Task DeleteActivityAsync(
        long stravaAthleteId,
        long stravaActivityId,
        CancellationToken ct = default);

    Task<Activity[]> GetActivitiesAsync(
        ActivityFilter filter,
        Paging? paging = null,
        CancellationToken ct = default);

    IObservable<FeedUpdate<Activity>> GetActivityFeedForAthlete(int athleteId);

    Task ImportActivitiesForAthleteAsync(int athleteId, DateTimeOffset from, CancellationToken ct = default);
    
    Task<PublicProfileStats?> GetPublicProfileStats(int athleteId, CancellationToken ct = default);
}
