namespace Tevling.Pages;

public partial class Activities : ComponentBase, IDisposable
{
    [Inject] private IActivityService ActivityService { get; set; } = null!;
    [Inject] private IAuthenticationService AuthenticationService { get; set; } = null!;
    [Inject] private ILogger<Activities> Logger { get; set; } = null!;
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;

    private const int PageSize = 50;
    private List<Activity> _activities = [];
    private IDisposable? _activityFeedSubscription;
    private int _page;
    private bool _showOnlyMine;
    private Activity[] ActivityList = [];
    private bool Importing { get; set; }
    private bool HasMore { get; set; } = true;
    private bool Reloading { get; set; }

    private bool ShowOnlyMine
    {
        get => _showOnlyMine;
        set
        {
            _showOnlyMine = value;
            OnFilterChange();
        }
    }

    private Athlete Athlete { get; set; } = default!;

    public void Dispose()
    {
        _activityFeedSubscription?.Dispose();
    }

    protected override async Task OnInitializedAsync()
    {
        Athlete = await AuthenticationService.GetCurrentAthleteAsync();

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
            Athlete.Id,
            !ShowOnlyMine);
        Activity[] activities = await ActivityService.GetActivitiesAsync(filter, new Paging(PageSize, _page), ct);
        AddActivities(activities);
    }

    private void SubscribeToActivityFeed()
    {
        _activityFeedSubscription = ActivityService.GetActivityFeedForAthlete(Athlete.Id)
            .Catch<FeedUpdate<Activity>, Exception>(
                err =>
                {
                    Logger.LogError(err, "Error in activity feed");
                    return Observable.Throw<FeedUpdate<Activity>>(err).Delay(TimeSpan.FromSeconds(1));
                })
            .Retry()
            .Subscribe(
                feed =>
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
            .Where(activity => !ShowOnlyMine || activity.AthleteId == Athlete.Id)
            .OrderByDescending(activity => activity.Details.StartDate)
            .ToArray();

        InvokeAsync(StateHasChanged);
    }
}
