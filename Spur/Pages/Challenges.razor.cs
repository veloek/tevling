namespace Spur.Pages;

public partial class Challenges : ComponentBase
{

    [Inject]
    IAuthenticationService AuthenticationService { get; set; } = null!;

    [Inject]
    IChallengeService ChallengeService { get; set; } = null!;

    [Inject]
    ILogger<Challenges> Logger { get; set; } = null!;

    private Challenge[] ChallengeList { get; set; } = [];
    private List<Challenge> _challenges = new();
    private int AthleteId { get; set; }
    private bool _showAllChallenges = true;
    private bool ShowAllChallenges
    {
        get => _showAllChallenges;
        set
        {
            _showAllChallenges = value;
            UpdateChallenges();
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
    }

    private async Task<bool> LoadMore(CancellationToken ct)
    {
        int prevCount = _challenges.Count;
        _page++;
        await FetchChallenges(ct);
        return _challenges.Count > prevCount;
    }

    private async Task FetchChallenges(CancellationToken ct = default)
    {
        Challenge[] challenges = await ChallengeService.GetChallengesAsync(_pageSize, _page, ct);
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
            .Where(c => ShowAllChallenges
                || (c.Athletes?.Any(athlete => athlete.Id == AthleteId) == true
                || c.CreatedById == AthleteId))
            .OrderByDescending(c => c.Start)
            .ThenBy(c => c.Title)
            .ToArray();

        InvokeAsync(StateHasChanged);
    }
}
