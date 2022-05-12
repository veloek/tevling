using Spur.Clients;
using Spur.Data;
using Spur.Model;

namespace Spur.Services;

public class ActivityService : IActivityService
{
    private readonly ILogger<ActivityService> _logger;
    private readonly IActivityRepository _activityRepository;
    private readonly IAthleteRepository _athleteRepository;
    private readonly IStravaClient _stravaClient;

    public ActivityService(
        ILogger<ActivityService> logger,
        IActivityRepository activityRepository,
        IAthleteRepository athleteRepository,
        IStravaClient stravaClient)
    {
        _logger = logger;
        _activityRepository = activityRepository;
        _athleteRepository = athleteRepository;
        _stravaClient = stravaClient;
    }

    public async Task<Activity> CreateActivityAsync(long stravaAthleteId, long stravaActivityId,
        CancellationToken ct = default)
    {
        var athlete = await _athleteRepository.GetAthleteByStravaIdAsync(stravaAthleteId, ct);
        if (athlete == null)
        {
            throw new Exception($"Unknown athlete ID {stravaAthleteId}");
        }

        _logger.LogInformation($"Adding activity ID {stravaActivityId} for athlete {athlete.Id}");
        var activity = await _activityRepository.AddActivityAsync(athlete.Id, stravaActivityId, ct);

        _logger.LogInformation($"Fetching activity details for activity ID {stravaActivityId}");
        var activityDetails = await FetchActivityDetailsAsync(activity, CancellationToken.None);

        activity.Details = activityDetails;
        activity = await _activityRepository.UpdateActivityAsync(activity, CancellationToken.None);

        return activity;
    }

    public async Task<Activity> UpdateActivityAsync(long stravaAthleteId, long stravaActivityId,
        CancellationToken ct = default)
    {
        var athlete = await _athleteRepository.GetAthleteByStravaIdAsync(stravaAthleteId, ct);
        if (athlete == null)
        {
            throw new Exception($"Unknown athlete ID {stravaAthleteId}");
        }

        var activity = await _activityRepository.GetActivityAsync(athlete.Id, stravaActivityId, ct);
        if (activity == null)
        {
            throw new Exception($"Unknown activity ID {stravaActivityId}");
        }

        _logger.LogInformation($"Fetching activity details for activity ID {stravaActivityId}");
        var activityDetails = await FetchActivityDetailsAsync(activity, CancellationToken.None);

        activity.Details = activityDetails;
        activity = await _activityRepository.UpdateActivityAsync(activity, CancellationToken.None);

        return activity;
    }

    public async Task DeleteActivityAsync(long stravaAthleteId, long stravaActivityId,
        CancellationToken ct = default)
    {
        var athlete = await _athleteRepository.GetAthleteByStravaIdAsync(stravaAthleteId, ct);
        if (athlete == null)
        {
            _logger.LogWarning($"Received activity update for unknown athlete ID {stravaAthleteId}");
            return;
        }

        var activity = await _activityRepository.GetActivityAsync(athlete.Id, stravaActivityId, ct);
        if (activity == null)
        {
            _logger.LogWarning($"Received activity update for unknown activity ID {stravaActivityId}");
            return;
        }

        await _activityRepository.RemoveActivityAsync(activity, ct);
    }

    private async Task<ActivityDetails> FetchActivityDetailsAsync(Activity activity, CancellationToken ct = default)
    {
        // TODO: Use athlete's access token / refresh token to call Strava API
        //       and get activity details
        throw new NotImplementedException();
    }
}
