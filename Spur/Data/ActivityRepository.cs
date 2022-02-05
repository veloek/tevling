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

    public async Task<Activity> AddActivityAsync(int athleteId, long stravaId,
        CancellationToken ct = default)
    {
        var activity = await _dataContext.AddActivityAsync(new Activity
        {
            StravaId = stravaId,
            AthleteId = athleteId
        }, ct);

        return activity;
    }
}
