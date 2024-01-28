namespace Tevling.Pages;

public partial class Athletes : ComponentBase, IDisposable
{
    [Inject]
    IAthleteService AthleteService { get; set; } = null!;

    [Inject]
    IAuthenticationService AuthenticationService { get; set; } = null!;

    [Inject]
    ILogger<Athletes> Logger { get; set; } = null!;

    private bool _showOnlyFollowing;
    private bool ShowOnlyFollowing
    {
        get => _showOnlyFollowing;
        set
        {
            _showOnlyFollowing = value;
            OnFilterChange();
        }
    }
    private Athlete[] AthleteList { get; set; } = [];
    private Athlete Athlete { get; set; } = default!;
    private bool HasMore { get; set; } = true;
    private List<Athlete> _athletes = [];
    private IDisposable? _athleteFeedSubscription;
    private int _pageSize = 10;
    private int _page = 0;

    protected override async Task OnInitializedAsync()
    {
        Athlete = await AuthenticationService.GetCurrentAthleteAsync();
        await FetchAthletes();
        SubscribeToAthleteFeed();
    }


    private void OnFilterChange()
    {
        _athletes = [];
        _page = -1;
        HasMore = true;
        UpdateAthletes();
    }

    private async Task LoadMore(CancellationToken ct)
    {
        int prevCount = _athletes.Count;
        _page++;
        await FetchAthletes(ct);
        HasMore = _athletes.Count > prevCount;
        StateHasChanged();
    }

    private async Task FetchAthletes(CancellationToken ct = default)
    {
        AthleteFilter filter = new(ShowOnlyFollowing ? Athlete.Id : null);
        Athlete[] athletes = await AthleteService.GetAthletesAsync(filter, new(_pageSize, _page), ct);
        AddAthletes(athletes);
    }

    private async Task ToggleFollowing(int followingId)
    {
        Athlete = await AthleteService.ToggleFollowingAsync(Athlete, followingId);
        await InvokeAsync(StateHasChanged);
    }

    private void SubscribeToAthleteFeed()
    {
        _athleteFeedSubscription = AthleteService.GetAthleteFeed()
            .Catch<FeedUpdate<Athlete>, Exception>(err =>
            {
                Logger.LogError(err, "Error in athlete feed");
                return Observable.Throw<FeedUpdate<Athlete>>(err).Delay(TimeSpan.FromSeconds(1));
            })
            .Retry()
            .Subscribe(feed =>
            {
                switch (feed.Action)
                {
                    case FeedAction.Create:
                        AddAthletes(feed.Item);
                        break;
                    case FeedAction.Update:
                        ReplaceAthlete(feed.Item);
                        break;
                    case FeedAction.Delete:
                        RemoveAthlete(feed.Item);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException("Unknown athlete feed action: " + feed.Action);
                }
            });
    }

    private void AddAthletes(params Athlete[] athletes)
    {
        _athletes.AddRange(athletes);

        UpdateAthletes();
    }

    private void ReplaceAthlete(Athlete athlete)
    {
        _athletes.RemoveAll(a => a.Id == athlete.Id);
        _athletes.Add(athlete);

        UpdateAthletes();
    }

    private void RemoveAthlete(Athlete athlete)
    {
        _athletes.RemoveAll(a => a.Id == athlete.Id);

        UpdateAthletes();
    }

    private void UpdateAthletes()
    {
        AthleteList = _athletes
            .Where(athlete => athlete.Id != Athlete.Id)
            .Where(athlete => !ShowOnlyFollowing || Athlete.IsFollowing(athlete.Id))
            .OrderBy(athlete => athlete.Name)
            .ToArray();

        InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        _athleteFeedSubscription?.Dispose();
    }
}
