using System.Reactive.Subjects;

namespace Tevling.Pages;

public partial class Challenges : ComponentBase, IDisposable
{
    [Inject]
    private ILogger<Challenges> Logger { get; set; } = null!;
    [Inject]
    private IAuthenticationService AuthenticationService { get; set; } = null!;
    [Inject]
    private IChallengeService ChallengeService { get; set; } = null!;

    private Challenge[] ChallengeList { get; set; } = [];
    private List<Challenge> _challenges = [];
    private int AthleteId { get; set; }
    private bool HasMore { get; set; } = true;
    private readonly TimeSpan _filterTextThrottle = TimeSpan.FromMilliseconds(300);
    private readonly Subject<string> _filterTextSubject = new();
    private IDisposable? _filterTextsubscription;
    private bool _showAllChallenges = true;
    private bool ShowAllChallenges
    {
        get => _showAllChallenges;
        set
        {
            _showAllChallenges = value;
            OnFilterChange();
        }
    }
    private bool _showOutdatedChallenges;
    private bool ShowOutdatedChallenges
    {
        get => _showOutdatedChallenges;
        set
        {
            _showOutdatedChallenges = value;
            OnFilterChange();
        }
    }
    private string _filterText = string.Empty;
    private string FilterText
    {
        get => _filterText;
        set
        {
            _filterText = value;
            OnFilterChange();
        }
    }

    private IDisposable? _challengeFeedSubscription;
    private int _pageSize = 10;
    private int _page = 0;

    protected override async Task OnInitializedAsync()
    {
        Athlete? athlete = await AuthenticationService.GetCurrentAthleteAsync();

        if (athlete != null)
        {
            AthleteId = athlete.Id;
            await FetchChallenges();
            SubscribeToChallengeFeed();
        }

        _filterTextsubscription = _filterTextSubject
            .Throttle(_filterTextThrottle)
            .Subscribe(s =>
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
            SearchText: _filterText,
            ByAthleteId: _showAllChallenges ? null : AthleteId,
            IncludeOutdatedChallenges: _showOutdatedChallenges);
        Challenge[] challenges = await ChallengeService.GetChallengesAsync(filter, _pageSize, _page, ct);
        AddChallenges(challenges);
    }

    private void SubscribeToChallengeFeed()
    {
        _challengeFeedSubscription = ChallengeService.GetChallengeFeed()
            .Catch<FeedUpdate<Challenge>, Exception>(err =>
            {
                Logger.LogError(err, "Error in challenge feed");
                return Observable.Throw<FeedUpdate<Challenge>>(err).Delay(TimeSpan.FromSeconds(1));
            })
            .Retry()
            .Subscribe(feed =>
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
        ChallengeList = _challenges
            .Where(c => _showAllChallenges
                || c.Athletes?.Any(athlete => athlete.Id == AthleteId) == true
                || c.CreatedById == AthleteId)
            .Where(c => _showOutdatedChallenges
                || c.End.UtcDateTime.Date >= DateTimeOffset.UtcNow.Date)
            .Where(c => string.IsNullOrWhiteSpace(_filterText)
                || c.Title.Contains(_filterText, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(c => c.Start)
            .ThenBy(c => c.Title)
            .ToArray();

        InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        _filterTextsubscription?.Dispose();
        _challengeFeedSubscription?.Dispose();
    }
}
