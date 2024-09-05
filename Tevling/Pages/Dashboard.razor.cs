namespace Tevling.Pages;

public partial class Dashboard : ComponentBase
{
    [Inject] private IAuthenticationService AuthenticationService { get; set; } = null!;
    [Inject] private IBrowserTime BrowserTime { get; set; } = null!;
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;
    [Inject] private IAthleteService AthleteService { get; set; } = null!;
    [Inject] private IChallengeService ChallengeService { get; set; } = null!;

    private string Greeting { get; set; } = string.Empty;
    private Athlete Athlete { get; set; } = default!;
    private Dictionary<Challenge, (string, string)> ActiveChallenges { get; } = [];
    private Dictionary<Challenge, (string, string)> RecentOutdatedChallenges { get; } = [];
    private IEnumerable<Athlete> SuggestedAthletes { get; set; } = [];

    protected override async Task OnInitializedAsync()
    {
        // Redirect from / to /activities making this the default page
        string relativePath = NavigationManager.ToBaseRelativePath(NavigationManager.Uri);

        if (!relativePath.StartsWith("dashboard")) NavigationManager.NavigateTo("dashboard", replace: true);

        Athlete = await AuthenticationService.GetCurrentAthleteAsync();


        await FetchActiveChallenges();
        await FetchRecentOutdatedChallenges();
        await FetchSuggestedAthletes();
        Greeting = await GetGreeting();
    }

    private async Task<string> GetGreeting(CancellationToken ct = default)
    {
        string athleteFirstName = Athlete.Name.Split(' ')[0];
        DateTimeOffset browserTime = await BrowserTime.ConvertToLocal(DateTimeOffset.Now, ct);

        return browserTime.Hour switch
        {
            < 12 => $"Good morning, {athleteFirstName}! ☀️",
            < 18 => $"Good afternoon, {athleteFirstName}! ☕️",
            _ => $"Good evening, {athleteFirstName}! 🌙",
        };
    }

    private async Task FetchActiveChallenges(CancellationToken ct = default)
    {
        DateTimeOffset browserTime = await BrowserTime.ConvertToLocal(DateTimeOffset.Now, ct);
        ChallengeFilter filter = new(
            string.Empty,
            null,
            OnlyJoinedChallenges: true,
            IncludeOutdatedChallenges: false);
        Challenge[] challenges = await ChallengeService.GetChallengesAsync(Athlete.Id, filter, null, ct);

        foreach (Challenge challenge in challenges)
        {
            if (challenge.End <= browserTime) continue;

            ScoreBoard scoreBoard = await ChallengeService.GetScoreBoardAsync(challenge.Id, ct);

            AthleteScore? score = scoreBoard.Scores.FirstOrDefault(x => x.Name == Athlete.Name);
            if (score is null) continue;

            int placement = scoreBoard.Scores.ToList().IndexOf(score) + 1;
            if (placement is 0) continue;

            string placementString = placement switch
            {
                1 => "1st 🥇",
                2 => "2nd 🥈",
                3 => "3rd 🥉",
                _ => $"{placement}th",
            };

            ActiveChallenges[challenge] = (placementString, score.Score);
        }
    }

    private async Task FetchRecentOutdatedChallenges(CancellationToken ct = default)
    {
        ChallengeFilter filter = new(
            string.Empty,
            null,
            OnlyJoinedChallenges: true,
            IncludeOutdatedChallenges: true);

        Challenge[] challenges = await ChallengeService.GetChallengesAsync(Athlete.Id, filter, null, ct);
        challenges =
        [
            .. challenges.Where(challenge => challenge.End <= DateTime.Now)
                .OrderByDescending(challenge => challenge.End)
                .Take(5),
        ];

        foreach (Challenge challenge in challenges)
        {
            ScoreBoard scoreBoard = await ChallengeService.GetScoreBoardAsync(challenge.Id, ct);
            AthleteScore? score = scoreBoard.Scores.FirstOrDefault(x => x.Name == Athlete.Name);
            if (score is null) continue;

            int placement = scoreBoard.Scores.ToList().IndexOf(score) + 1;
            if (placement is 0) continue;

            string placementString = placement switch
            {
                1 => "1st 🥇",
                2 => "2nd 🥈",
                3 => "3rd 🥉",
                _ => $"{placement}th",
            };

            RecentOutdatedChallenges[challenge] = (placementString, score.Score);
        }
    }


    private async Task FetchSuggestedAthletes(CancellationToken ct = default)
    {
        SuggestedAthletes = await AthleteService.GetSuggestedAthletesToFollowAsync(Athlete.Id, ct);
    }

    private async Task ToggleFollowing(int followingId)
    {
        Athlete = await AthleteService.ToggleFollowingAsync(Athlete, followingId);
        await InvokeAsync(StateHasChanged);
    }
}
