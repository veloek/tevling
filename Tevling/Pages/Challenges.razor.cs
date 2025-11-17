using System.Reactive.Subjects;
using Tevling.Shared;
using Tevling.Strava;

namespace Tevling.Pages;

public partial class Challenges : ComponentBase, IDisposable
{
    [Inject] private ILogger<Challenges> Logger { get; set; } = null!;
    [Inject] private IAuthenticationService AuthenticationService { get; set; } = null!;
    [Inject] private IChallengeService ChallengeService { get; set; } = null!;

    private readonly Subject<string> _filterTextSubject = new();
    private readonly TimeSpan _filterTextThrottle = TimeSpan.FromMilliseconds(300);

    private readonly int _pageSize = 10;
    private IDisposable? _challengeFeedSubscription;
    private List<Challenge> _challenges = [];

    private string _filterText = string.Empty;
    private IDisposable? _filterTextSubscription;
    private int _page;

    private bool _showAllChallenges = true;

    private bool _showOutdatedChallenges;
    private bool _showTimeChallenges = true;
    private bool _showElevationChallenges = true;
    private bool _showDistanceChallenges = true;
    private ICollection<ActivityType> _activityTypes= [];
    private DropdownSearch<ActivityType>? _dropdownSearchRefActivityTypes;
    private static IEnumerable<ActivityType> ActivityTypes => Enum.GetValues<ActivityType>();


    private int AthleteId { get; set; }
    private bool HasMore { get; set; } = true;
    private Challenge[] ChallengeList { get; set; } = [];

    private ICollection<ActivityType> SelectedActivityTypes
    {
        get => _activityTypes;
        set
        {
            _activityTypes = value;
            OnFilterChange();
        }
    }

    private bool ShowAllChallenges
    {
        get => _showAllChallenges;
        set
        {
            _showAllChallenges = value;
            OnFilterChange();
        }
    }

    private bool ShowOutdatedChallenges
    {
        get => _showOutdatedChallenges;
        set
        {
            _showOutdatedChallenges = value;
            OnFilterChange();
        }
    }
    private bool ShowTimeChallenges
    {
        get => _showTimeChallenges;
        set
        {
            _showTimeChallenges = value;
            OnFilterChange();
        }
    }
    private bool ShowElevationChallenges
    {
        get => _showElevationChallenges;
        set
        {
            _showElevationChallenges = value;
            OnFilterChange();
        }
    }
    private bool ShowDistanceChallenges
    {
        get => _showDistanceChallenges;
        set
        {
            _showDistanceChallenges = value;
            OnFilterChange();
        }
    }

    private string FilterText
    {
        get => _filterText;
        set
        {
            _filterText = value;
            OnFilterChange();
        }
    }

    private async Task DeselectActivityType(ActivityType item)
    {
        if (_dropdownSearchRefActivityTypes is null) return;

        await _dropdownSearchRefActivityTypes.DeselectItemAsync(item);
    }

    public void Dispose()
    {
        _filterTextSubscription?.Dispose();
        _challengeFeedSubscription?.Dispose();
    }

    protected override async Task OnInitializedAsync()
    {
        Athlete athlete = await AuthenticationService.GetCurrentAthleteAsync();

        AthleteId = athlete.Id;
        await FetchChallenges();
        SubscribeToChallengeFeed();

        _filterTextSubscription = _filterTextSubject
            .Throttle(_filterTextThrottle)
            .Subscribe(
                s =>
                {
                    FilterText = s;
                    InvokeAsync(StateHasChanged);
                });
    }

    private void SetFilterTextDebounced(ChangeEventArgs e)
    {
        _filterTextSubject.OnNext(e.Value!.ToString()!);
    }

    private void OnFilterChange()
    {
        _challenges = [];
        _page = -1;
        HasMore = true;
        UpdateChallenges();
    }

    private async Task LoadMore(CancellationToken ct)
    {
        int prevCount = _challenges.Count;
        _page++;
        await FetchChallenges(ct);
        HasMore = _challenges.Count > prevCount;
        StateHasChanged();
    }

    private async Task FetchChallenges(CancellationToken ct = default)
    {
        ChallengeFilter filter = new(
            _filterText,
            _showAllChallenges ? null : AthleteId,
            _showOutdatedChallenges,
            !_showAllChallenges,
            _showTimeChallenges,
            _showElevationChallenges,
            _showDistanceChallenges,
            [.. _activityTypes]);
        Challenge[] challenges =
            await ChallengeService.GetChallengesAsync(AthleteId, filter, new Paging(_pageSize, _page), ct);
        AddChallenges(challenges);
    }

    private void SubscribeToChallengeFeed()
    {
        _challengeFeedSubscription = ChallengeService.GetChallengeFeed(AthleteId)
            .Catch<FeedUpdate<Challenge>, Exception>(
                err =>
                {
                    Logger.LogError(err, "Error in challenge feed");
                    return Observable.Throw<FeedUpdate<Challenge>>(err).Delay(TimeSpan.FromSeconds(1));
                })
            .Retry()
            .Subscribe(
                feed =>
                {
                    switch (feed.Action)
                    {
                        case FeedAction.Create:
                            AddChallenges(feed.Item);
                            break;
                        case FeedAction.Update:
                            ReplaceChallenge(feed.Item);
                            break;
                        case FeedAction.Delete:
                            RemoveChallenge(feed.Item);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException("Unknown challenge feed action: " + feed.Action);
                    }
                });
    }

    private void AddChallenges(params Challenge[] challenges)
    {
        _challenges.AddRange(challenges);

        UpdateChallenges();
    }

    private void ReplaceChallenge(Challenge challenge)
    {
        _challenges.RemoveAll(c => c.Id == challenge.Id);
        _challenges.Add(challenge);

        UpdateChallenges();
    }

    private void RemoveChallenge(Challenge challenge)
    {
        _challenges.RemoveAll(c => c.Id == challenge.Id);

        UpdateChallenges();
    }

    private void UpdateChallenges()
    {
        ChallengeList =
        [
            .. _challenges
                .Where(
                    c => _showAllChallenges ||
                        c.Athletes?.Any(athlete => athlete.Id == AthleteId) == true ||
                        c.CreatedById == AthleteId)
                .Where(c => _showOutdatedChallenges || c.End.UtcDateTime.Date >= DateTimeOffset.UtcNow.Date)
                .Where(
                    c =>
                        (_showTimeChallenges && c.Measurement == ChallengeMeasurement.Time) ||
                        (_showElevationChallenges && c.Measurement == ChallengeMeasurement.Elevation) ||
                        (_showDistanceChallenges && c.Measurement == ChallengeMeasurement.Distance))
                .Where(
                    c => _activityTypes.Count <= 0 || _activityTypes.Intersect(c.ActivityTypes).Any())
                .Where(
                    c => string.IsNullOrWhiteSpace(_filterText) ||
                        c.Title.Contains(_filterText, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(c => c.Start)
                .ThenBy(c => c.Title),
        ];

        InvokeAsync(StateHasChanged);
    }
}
