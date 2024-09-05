using System.Reactive.Subjects;
using Microsoft.EntityFrameworkCore;
using Tevling.Strava;

namespace Tevling.Services;

public class ActivityService : IActivityService
{
    private readonly ILogger<ActivityService> _logger;
    private readonly IDbContextFactory<DataContext> _dataContextFactory;
    private readonly IAthleteService _athleteService;
    private readonly IStravaClient _stravaClient;
    private readonly Subject<FeedUpdate<Activity>> _activityFeed = new();

    public ActivityService(
        ILogger<ActivityService> logger,
        IDbContextFactory<DataContext> dataContextFactory,
        IAthleteService athleteService,
        IStravaClient stravaClient)
    {
        _logger = logger;
        _dataContextFactory = dataContextFactory;
        _athleteService = athleteService;
        _stravaClient = stravaClient;
    }

    public async Task<Activity> CreateActivityAsync(long stravaAthleteId, long stravaActivityId,
        CancellationToken ct = default)
    {
        using DataContext dataContext = await _dataContextFactory.CreateDbContextAsync(ct);

        Athlete athlete = await dataContext.Athletes
            .FirstOrDefaultAsync(a => a.StravaId == stravaAthleteId, ct) ??
            throw new Exception($"Unknown Strava athlete ID {stravaAthleteId}");

        Activity? existingActivity = await dataContext.Activities
            .Include(a => a.Details) // TODO: Is this needed?
            .Include(a => a.Athlete)
            .FirstOrDefaultAsync(a => a.AthleteId == athlete.Id && a.StravaId == stravaActivityId, ct);

        if (existingActivity != null)
        {
            _logger.LogInformation("Skipping duplicate activity with Strava ID: {StravaId}", stravaActivityId);
            return existingActivity;
        }

        _logger.LogInformation("Adding Strava activity ID {StravaActivityId} for athlete ID {AthleteId}", stravaActivityId, athlete.Id);
        Activity activity = await dataContext.AddActivityAsync(new Activity()
        {
            StravaId = stravaActivityId,
            AthleteId = athlete.Id,
        }, ct);

        // Since tracking is disabled by default, we need to manually load the athlete for it to
        // be present in the feed update.
        await dataContext.Entry(activity).Reference(a => a.Athlete).LoadAsync(ct);

        _logger.LogDebug("Fetching activity details for Strava activity ID {StravaActivityId}", stravaActivityId);
        ActivityDetails activityDetails = await FetchActivityDetailsAsync(activity, CancellationToken.None);

        activity.Details = activityDetails;
        activity = await dataContext.UpdateActivityAsync(activity, CancellationToken.None);

        _activityFeed.OnNext(new FeedUpdate<Activity> { Item = activity, Action = FeedAction.Create });

        return activity;
    }

    public async Task<Activity> UpdateActivityAsync(long stravaAthleteId, long stravaActivityId,
        CancellationToken ct = default)
    {
        using DataContext dataContext = await _dataContextFactory.CreateDbContextAsync(ct);

        Athlete athlete = await dataContext.Athletes
            .FirstOrDefaultAsync(a => a.StravaId == stravaAthleteId, ct) ??
            throw new Exception($"Unknown Strava athlete ID {stravaAthleteId}");

        Activity activity = await dataContext.Activities
            .Include(a => a.Athlete)
            .FirstOrDefaultAsync(a => a.AthleteId == athlete.Id && a.StravaId == stravaActivityId, ct) ??
            throw new Exception($"Unknown Strava activity ID {stravaActivityId}");

        _logger.LogDebug("Fetching activity details for Strava activity ID {StravaActivityId}", stravaActivityId);
        ActivityDetails activityDetails = await FetchActivityDetailsAsync(activity, CancellationToken.None);

        _logger.LogInformation("Updating Strava activity ID {StravaActivityId} for athlete {AthleteId}", stravaActivityId, athlete.Id);
        activity.Details = activityDetails;
        activity = await dataContext.UpdateActivityAsync(activity, CancellationToken.None);

        _activityFeed.OnNext(new FeedUpdate<Activity> { Item = activity, Action = FeedAction.Update });

        return activity;
    }

    public async Task DeleteActivityAsync(long stravaAthleteId, long stravaActivityId,
        CancellationToken ct = default)
    {
        using DataContext dataContext = await _dataContextFactory.CreateDbContextAsync(ct);

        Athlete athlete = await dataContext.Athletes
            .FirstOrDefaultAsync(a => a.StravaId == stravaAthleteId, ct) ??
            throw new Exception($"Unknown Strava athlete ID {stravaAthleteId}");

        Activity? activity = await dataContext.Activities
            .FirstOrDefaultAsync(a => a.AthleteId == athlete.Id && a.StravaId == stravaActivityId, ct) ??
            throw new Exception($"Unknown Strava activity ID {stravaActivityId}");

        _logger.LogInformation("Deleting Strava activity ID {StravaActivityId} for athlete {AthleteId}", stravaActivityId, athlete.Id);
        _ = await dataContext.RemoveActivityAsync(activity, ct);

        _activityFeed.OnNext(new FeedUpdate<Activity> { Item = activity, Action = FeedAction.Delete });
    }

    public async Task<Activity[]> GetActivitiesAsync(ActivityFilter filter, Paging? paging = null,
        CancellationToken ct = default)
    {
        using DataContext dataContext = await _dataContextFactory.CreateDbContextAsync(ct);

        Athlete athlete = await dataContext.Athletes
            .AsQueryable()
            .If(filter.IncludeFollowing, q => q.Include(a => a.Following))
            .FirstOrDefaultAsync(a => a.Id == filter.AthleteId, ct)
                ?? throw new Exception($"Unknown athlete ID {filter.AthleteId}");

        Activity[] activities = await dataContext.Activities
            .Include(a => a.Athlete)
            .ThenInclude(a => a!.Following)
            .Where(activity => activity.AthleteId == athlete.Id
                            || (filter.IncludeFollowing
                                && athlete.Following!.Select(a => a.Id).Contains(activity.AthleteId)))
            .OrderByDescending(activity => activity.Details.StartDate)
            .ThenBy(activity => activity.Id) // Need stable sorting for paging
            .If(paging != null, x => x.Skip(paging!.Value.Page * paging!.Value.PageSize), x => (IQueryable<Activity>)x)
            .If(paging != null, x => x.Take(paging!.Value.PageSize))
            .ToArrayAsync(ct);

        return activities;
    }

    public IObservable<FeedUpdate<Activity>> GetActivityFeedForAthlete(int athleteId)
    {
        return Observable
            .FromAsync(async ct =>
            {
                using DataContext dataContext = await _dataContextFactory.CreateDbContextAsync(ct);

                Athlete? athlete = await dataContext.Athletes
                    .Include(a => a.Following)
                    .FirstOrDefaultAsync(a => a.Id == athleteId, ct);

                return athlete;
            })
            .SelectMany(athlete =>
            {
                if (athlete is null)
                {
                    return Observable.Throw<FeedUpdate<Activity>>(new ArgumentException($"Athlete {athleteId} not found"));
                }
                return _activityFeed.Where(a => a.Item.AthleteId == athlete.Id || athlete.IsFollowing(a.Item.AthleteId));
            });
    }

    public async Task ImportActivitiesForAthleteAsync(int athleteId, DateTimeOffset from, CancellationToken ct = default)
    {
        using DataContext dataContext = await _dataContextFactory.CreateDbContextAsync(ct);

        Athlete? athlete = await _athleteService.GetAthleteByIdAsync(athleteId, ct);
        if (athlete is null)
        {
            _logger.LogWarning("Athlete {AthleteId} not found", athleteId);
            return;
        }

        string accessToken = await _athleteService.GetAccessTokenAsync(athleteId, ct);
        int page = 1;
        int pageSize = 30;
        SummaryActivity[] activities;

        _logger.LogInformation("Importing activities for athlete {AthleteId} starting from {From}", athleteId, from);
        do
        {
            _logger.LogInformation("Fetching page {Page} of import", page);

            activities = await _stravaClient.GetAthleteActivitiesAsync(
                accessToken, after: from, page: page, pageSize: pageSize, ct: ct);

            foreach (SummaryActivity stravaActivity in activities)
            {
                bool existingActivity = await dataContext.Activities.AnyAsync(a => a.StravaId == stravaActivity.Id, ct);

                if (existingActivity)
                {
                    await UpdateActivityAsync(athlete.StravaId, stravaActivity.Id, ct);
                }
                else
                {
                    await CreateActivityAsync(athlete.StravaId, stravaActivity.Id, ct);
                }
            }

            // Give the Strava API time to breathe before the next request
            if (activities.Length == pageSize)
            {
                await Task.Delay(TimeSpan.FromSeconds(1), ct);
            }

        } while (activities.Length == pageSize && page++ <= 10); // Fail safe at 10 pages max
    }

    private async Task<ActivityDetails> FetchActivityDetailsAsync(Activity activity, CancellationToken ct = default)
    {
        string accessToken = await _athleteService.GetAccessTokenAsync(activity.AthleteId, ct);
        DetailedActivity stravaActivity = await _stravaClient.GetActivityAsync(activity.StravaId, accessToken, ct);
        _logger.LogDebug("Strava activity details: {@Details}", stravaActivity);

        ActivityDetails activityDetails = MapActivityDetails(stravaActivity);

        return activityDetails;
    }

    private static ActivityDetails MapActivityDetails(DetailedActivity stravaActivity)
    {
        return new()
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
    }
}
