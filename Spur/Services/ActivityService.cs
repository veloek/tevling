using System.Reactive.Linq;
using System.Reactive.Subjects;
using Microsoft.EntityFrameworkCore;
using Spur.Clients;
using Spur.Data;
using Spur.Model;

namespace Spur.Services;

public class ActivityService : IActivityService
{
    private readonly ILogger<ActivityService> _logger;
    private readonly IDataContext _dataContext;
    private readonly IAthleteService _athleteService;
    private readonly IStravaClient _stravaClient;
    private readonly Subject<ActivityFeed> _activityFeed = new();

    public ActivityService(
        ILogger<ActivityService> logger,
        IDataContext dataContext,
        IAthleteService athleteService,
        IStravaClient stravaClient)
    {
        _logger = logger;
        _dataContext = dataContext;
        _athleteService = athleteService;
        _stravaClient = stravaClient;
    }

    public async Task<Activity> CreateActivityAsync(long stravaAthleteId, long stravaActivityId,
        CancellationToken ct = default)
    {
        Athlete athlete = await _dataContext.Athletes
            .FirstOrDefaultAsync(a => a.StravaId == stravaAthleteId, ct) ??
            throw new Exception($"Unknown athlete ID {stravaAthleteId}");

        _logger.LogInformation($"Adding activity ID {stravaActivityId} for athlete {athlete.Id}");
        Activity activity = await _dataContext.AddActivityAsync(new Activity()
        {
            StravaId = stravaActivityId,
            AthleteId = athlete.Id,
            Details = new ActivityDetails(),
        }, ct);

        _logger.LogDebug($"Fetching activity details for activity ID {stravaActivityId}");
        ActivityDetails activityDetails = await FetchActivityDetailsAsync(activity, CancellationToken.None);

        activity.Details = activityDetails;
        activity = await _dataContext.UpdateActivityAsync(activity, CancellationToken.None);

        _activityFeed.OnNext(new ActivityFeed { Activity = activity, Action = FeedAction.Create });

        return activity;
    }

    public async Task<Activity> UpdateActivityAsync(long stravaAthleteId, long stravaActivityId,
        CancellationToken ct = default)
    {
        Athlete athlete = await _dataContext.Athletes
            .FirstOrDefaultAsync(a => a.StravaId == stravaAthleteId, ct) ??
            throw new Exception($"Unknown athlete ID {stravaAthleteId}");

        Activity activity = await _dataContext.Activities
            .FirstOrDefaultAsync(a => a.AthleteId == athlete.Id && a.StravaId == stravaActivityId, ct) ??
            throw new Exception($"Unknown activity ID {stravaActivityId}");

        _logger.LogDebug($"Fetching activity details for activity ID {stravaActivityId}");
        ActivityDetails activityDetails = await FetchActivityDetailsAsync(activity, CancellationToken.None);

        _logger.LogInformation($"Updating activity ID {stravaActivityId} for athlete {athlete.Id}");
        activity.Details = activityDetails;
        activity = await _dataContext.UpdateActivityAsync(activity, CancellationToken.None);

        _activityFeed.OnNext(new ActivityFeed { Activity = activity, Action = FeedAction.Update });

        return activity;
    }

    public async Task DeleteActivityAsync(long stravaAthleteId, long stravaActivityId,
        CancellationToken ct = default)
    {
        Athlete athlete = await _dataContext.Athletes
            .FirstOrDefaultAsync(a => a.StravaId == stravaAthleteId, ct) ??
            throw new Exception($"Unknown athlete ID {stravaAthleteId}");

        Activity? activity = await _dataContext.Activities
            .FirstOrDefaultAsync(a => a.AthleteId == athlete.Id && a.StravaId == stravaActivityId, ct) ??
            throw new Exception($"Unknown activity ID {stravaActivityId}");

        _logger.LogInformation($"Deleting activity ID {stravaActivityId} for athlete {athlete.Id}");
        _ = await _dataContext.RemoveActivityAsync(activity, ct);

        _activityFeed.OnNext(new ActivityFeed { Activity = activity, Action = FeedAction.Delete });
    }

    public async Task<Activity[]> GetActivitiesForAthleteAsync(int athleteId, int pageSize, int page = 0,
        CancellationToken ct = default)
    {
        Athlete athlete = await _dataContext.Athletes
            .Include(a => a.Following)
            .FirstOrDefaultAsync(a => a.Id == athleteId, ct)
            ?? throw new Exception($"Unknown athlete ID {athleteId}");

        Activity[] activities = await _dataContext.Activities
            .Where(activity => activity.AthleteId == athlete.Id || athlete.Following!.Select(a => a.Id).Contains(activity.AthleteId))
            .OrderByDescending(activity => activity.Details.StartDate)
            .Skip(page * pageSize)
            .Take(pageSize)
            .ToArrayAsync(ct);

        return activities;
    }

    public IObservable<ActivityFeed> GetActivityFeedForAthlete(int athleteId)
    {
        return Observable
            .FromAsync(ct => _dataContext.Athletes
                .Include(a => a.Following)
                .FirstOrDefaultAsync(a => a.Id == athleteId, ct))
            .SelectMany(athlete =>
            {
                if (athlete is null)
                {
                    return Observable.Throw<ActivityFeed>(new ArgumentException($"Athlete {athleteId} not found"));
                }
                return _activityFeed.Where(a => a.Activity.AthleteId == athlete.Id || athlete.IsFollowing(a.Activity.AthleteId));
            });
    }

    private async Task<ActivityDetails> FetchActivityDetailsAsync(Activity activity, CancellationToken ct = default)
    {
        string accessToken = await _athleteService.GetAccessTokenAsync(activity.AthleteId, ct);
        Strava.Activity stravaActivity = await _stravaClient.GetActivityAsync(activity.StravaId, accessToken, ct);

        ActivityDetails activityDetails = new()
        {
            Name = stravaActivity.Name ?? string.Empty,
            Description = stravaActivity.Description,
            DistanceInMeters = stravaActivity.Distance,
            MovingTimeInSeconds = stravaActivity.MovingTime,
            ElapsedTimeInSeconds = stravaActivity.ElapsedTime,
            TotalElevationGain = stravaActivity.TotalElevationGain,
            Calories = stravaActivity.Calories,
            Type = stravaActivity.Type,
            StartDate = stravaActivity.StartDate,
            Manual = stravaActivity.Manual,
        };

        return activityDetails;
    }
}
