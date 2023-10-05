using Microsoft.EntityFrameworkCore;
using Spur.Model;

namespace Spur.Data;

public class ActivityRepository : IActivityRepository
{
    private readonly ILogger<ActivityRepository> _logger;
    private readonly IDataContext _dataContext;

    public ActivityRepository(
        ILogger<ActivityRepository> logger,
        IDataContext dataContext)
    {
        _logger = logger;
        _dataContext = dataContext;
    }

    public async Task<Activity?> GetActivityAsync(int athleteId, long stravaId,
        CancellationToken ct = default)
    {
        Activity? activity = await _dataContext.Activities
            .FirstOrDefaultAsync(a => a.AthleteId == athleteId && a.StravaId == stravaId, ct);

        return activity;
    }

    public async Task<Activity> AddActivityAsync(int athleteId, long stravaId,
        CancellationToken ct = default)
    {
        Activity activity = await _dataContext.AddActivityAsync(new Activity
        {
            StravaId = stravaId,
            AthleteId = athleteId
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
