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
    private bool Reloading { get; set; }
    private Activity[] ActivityList = [];
    private bool ShowOnlyMine
    {
        get => _showOnlyMine;
        set
        {
            _showOnlyMine = value;
            OnFilterChange();
        }
    }
    private bool _showOnlyMine;
    private Athlete _athlete { get; set; } = default!;
    private List<Activity> _activities = [];
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

        await FetchActivities();
        SubscribeToActivityFeed();
    }

    private void OnFilterChange()
    {
        _activities = [];
        _page = -1;
        HasMore = true;
        Reloading = true;
        UpdateActivities();
    }

    private async Task LoadMore(CancellationToken ct)
    {
        int prevCount = _activities.Count;
        _page++;
        await FetchActivities(ct);
        HasMore = _activities.Count > prevCount;
        Reloading = false;
        StateHasChanged();
    }

    private async Task FetchActivities(CancellationToken ct = default)
    {
        ActivityFilter filter = new(
            AthleteId: _athlete.Id,
            IncludeFollowing: !ShowOnlyMine);
        Activity[] activities = await ActivityService.GetActivitiesAsync(filter, _pageSize, _page, ct);
        AddActivities(activities);
    }

    private void SubscribeToActivityFeed()
    {
        _activityFeedSubscription = ActivityService.GetActivityFeedForAthlete(_athlete.Id)
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
        // Even if activities from the DB are filtered and sorted, we need to
        // filter and sort here as well due to added activities from the feed.
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
