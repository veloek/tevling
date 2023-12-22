namespace Spur.Pages;

public partial class Activities : ComponentBase, IDisposable
{
    [Inject]
    IActivityService ActivityService { get; set; } = null!;

    [Inject]
    IAuthenticationService AuthenticationService { get; set; } = null!;

    [Inject]
    ILogger<Activities> Logger { get; set; } = null!;

    [Inject]
    NavigationManager NavigationManager { get; set; } = null!;

    private bool Importing { get; set; }
    private bool HasMore { get; set; } = true;
    private Activity[] ActivityList = [];
    private bool ShowOnlyMine
    {
        get => _showOnlyMine;
        set
        {
            _showOnlyMine = value;
            UpdateActivities();
        }
    }
    private bool _showOnlyMine;
    private Athlete _athlete { get; set; } = default!;
    private List<Activity> _activities = new();
    private IDisposable? _activityFeedSubscription;
    private int _pageSize = 50;
    private int _page = 0;

    protected override async Task OnInitializedAsync()
    {
        // Redirect from / to /activities making this the default page
        string relativePath = NavigationManager.ToBaseRelativePath(NavigationManager.Uri);

        if (!relativePath.StartsWith("activities"))
        {
            NavigationManager.NavigateTo("activities", replace: true);
        }

        _athlete = await AuthenticationService.GetCurrentAthleteAsync();

        await FetchActivities(_athlete.Id);
        SubscribeToActivityFeed(_athlete.Id);
    }

    private async Task LoadMore(CancellationToken ct)
    {
        int prevCount = _activities.Count;
        _page++;
        await FetchActivities(_athlete.Id);
        HasMore = _activities.Count > prevCount;
        StateHasChanged();
    }

    private async Task FetchActivities(int athleteId)
    {
        Activity[] activities = await ActivityService.GetActivitiesForAthleteAsync(athleteId, _pageSize, _page);
        AddActivities(activities);
    }

    private void SubscribeToActivityFeed(int athleteId)
    {
        _activityFeedSubscription = ActivityService.GetActivityFeedForAthlete(athleteId)
            .Catch<FeedUpdate<Activity>, Exception>(err =>
            {
                Logger.LogError(err, "Error in activity feed");
                return Observable.Throw<FeedUpdate<Activity>>(err).Delay(TimeSpan.FromSeconds(1));
            })
            .Retry()
            .Subscribe(feed =>
            {
                switch (feed.Action)
                {
                    case FeedAction.Create:
                        AddActivities(feed.Item);
                        break;
                    case FeedAction.Update:
                        ReplaceActivity(feed.Item);
                        break;
                    case FeedAction.Delete:
                        RemoveActivity(feed.Item);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException("Unknown activity feed action: " + feed.Action);
                }
            });
    }

    private void AddActivities(params Activity[] activities)
    {
        _activities.AddRange(activities);

        UpdateActivities();
    }

    private void ReplaceActivity(Activity activity)
    {
        _activities.RemoveAll(a => a.Id == activity.Id);
        _activities.Add(activity);

        UpdateActivities();
    }

    private void RemoveActivity(Activity activity)
    {
        _activities.RemoveAll(a => a.Id == activity.Id);

        UpdateActivities();
    }

    private void UpdateActivities()
    {
        ActivityList = _activities
            .Where(activity => !ShowOnlyMine || activity.AthleteId == _athlete.Id)
            .OrderByDescending(activity => activity.Details.StartDate)
            .ToArray();

        InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        _activityFeedSubscription?.Dispose();
    }
}
