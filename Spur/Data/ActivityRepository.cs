using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Spur.Model;

namespace Spur.Data;

public class ActivityRepository : IActivityRepository
{
    private readonly ILogger<ActivityRepository> _logger;
    private readonly IDataContext _dataContext;
    private readonly IAthleteRepository _athleteRepository;

    public ActivityRepository(
        ILogger<ActivityRepository> logger,
        IDataContext dataContext,
        IAthleteRepository athleteRepository)
    {
        _logger = logger;
        _dataContext = dataContext;
        _athleteRepository = athleteRepository;
    }

    public async Task<Activity?> GetActivityAsync(int athleteId, long stravaId,
        CancellationToken ct = default)
    {
        Activity? activity = await _dataContext.Activities
            .FirstOrDefaultAsync(a => a.AthleteId == athleteId && a.StravaId == stravaId, ct);

        return activity;
    }

    public async IAsyncEnumerable<Activity> GetActivitiesForAthlete(int athleteId,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        Athlete? athlete = await _athleteRepository.GetAthleteByIdAsync(athleteId, ct);

        if (athlete is null)
        {
            throw new ArgumentException($"Athlete {athleteId} not found");
        }

        IAsyncEnumerable<Activity> activities = _dataContext.Activities
            .Where(activity => activity.AthleteId == athlete.Id || athlete.Following!.Select(a => a.Id).Contains(activity.AthleteId))
            .OrderByDescending(activity => activity.Details.StartDate)
            .AsAsyncEnumerable();

        await foreach (Activity activity in activities.WithCancellation(ct))
        {
            yield return activity;
        }
    }

    public async Task<Activity> AddActivityAsync(int athleteId, long stravaId,
        CancellationToken ct = default)
    {
        Activity activity = await _dataContext.AddActivityAsync(new Activity
        {
            StravaId = stravaId,
            AthleteId = athleteId,
            Details = new ActivityDetails(),
        }, ct);

        return activity;
    }

    public Task<Activity> UpdateActivityAsync(Activity activity,
        CancellationToken ct = default)
    {
        return _dataContext.UpdateActivityAsync(activity, ct);
    }

    public Task<Activity> RemoveActivityAsync(Activity activity,
        CancellationToken ct = default)
    {
        return _dataContext.RemoveActivityAsync(activity, ct);
    }
}
