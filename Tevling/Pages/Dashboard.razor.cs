namespace Tevling.Pages;

public partial class Dashboard : ComponentBase {

    [Inject]
    IAuthenticationService AuthenticationService { get; set; } = null!;

    [Inject]
    IAthleteService AthleteService { get; set; } = null!;

    [Inject]
    IChallengeService ChallengeService { get; set; } = null!;


    private Athlete? Athlete { get; set; }
    private Dictionary<Challenge, (string, string)> ActiveChallenges { get; set; }  = [];
    private Dictionary<Challenge, (string, string)> RecentOutdatedChallenges { get; set; } = [];
    private IEnumerable<Athlete> SuggestedAthletes {get; set; } = [];
    
    protected override async Task OnInitializedAsync()
    {
        Athlete = await AuthenticationService.GetCurrentAthleteAsync();

        if (Athlete != null)
        {
            int athleteId = Athlete.Id;
            await FetchActiveChallenges();
            await FetchRecentOutdatedChallenges();
            await FetchSuggestedAthletes();
        }
    }
    
    private string GetGreeting() {
        if (Athlete is null) return string.Empty;

        string athleteFirstName = Athlete.Name.Split(' ')[0];
        return DateTime.Now.Hour switch {
            < 12 => $"Good morning, {athleteFirstName}! ☀️",
            < 18 => $"Good afternoon, {athleteFirstName}! ☕️",
            _ => $"Good evening, {athleteFirstName}! 🌙"
        };
    }

    private async Task FetchActiveChallenges(CancellationToken ct = default)
    {
        if (Athlete is null) return;
        ChallengeFilter filter = new(
            SearchText: string.Empty,
            ByAthleteId: null,
            OnlyJoinedChallenges: true,
            IncludeOutdatedChallenges: false);
        Challenge[] challenges = await ChallengeService.GetChallengesAsync(Athlete.Id, filter, null, ct);

        foreach (Challenge challenge in challenges) {
            if (challenge.End <= DateTime.Now) continue;

            ScoreBoard scoreBoard = await ChallengeService.GetScoreBoardAsync(challenge.Id, ct);
            if (scoreBoard is null) continue;

            AthleteScore? score = scoreBoard.Scores.FirstOrDefault(x => x.Name == Athlete.Name);
            if (score is null) continue;

            int placement = scoreBoard.Scores.ToList().IndexOf(score) + 1;
            if (placement is 0) continue;

            string placementString = placement switch {
                1 => "1st 🥇",
                2 => "2nd 🥈",
                3 => "3rd 🥉",
                _ => $"{placement}th"
            };

            ActiveChallenges[challenge] = (placementString, score.Score);
        }
    }

    private async Task FetchRecentOutdatedChallenges(CancellationToken ct = default)
    {
        if (Athlete is null) return;
        ChallengeFilter filter = new(
            SearchText: string.Empty,
            ByAthleteId: null,
            OnlyJoinedChallenges: true,
            IncludeOutdatedChallenges: true);
        
        Challenge[] challenges = await ChallengeService.GetChallengesAsync(Athlete.Id, filter, null, ct);
        challenges = [.. challenges.Where(challenge => challenge.End <= DateTime.Now).OrderByDescending(challenge => challenge.End)];
        
        foreach (Challenge challenge in challenges) {
            ScoreBoard scoreBoard = await ChallengeService.GetScoreBoardAsync(challenge.Id, ct);
            if (scoreBoard is null) continue;

            AthleteScore? score = scoreBoard.Scores.FirstOrDefault(x => x.Name == Athlete.Name);
            if (score is null) continue;

            int placement = scoreBoard.Scores.ToList().IndexOf(score) + 1;
            if (placement is 0) continue;

            string placementString = placement switch {
                1 => "1st 🥇",
                2 => "2nd 🥈",
                3 => "3rd 🥉",
                _ => $"{placement}th"
            };

            RecentOutdatedChallenges[challenge] = (placementString, score.Score);
        }
    }


    private async Task FetchSuggestedAthletes(CancellationToken ct = default)
    {
        if (Athlete is null) return;

        SuggestedAthletes = await AthleteService.GetSuggestedAthletesToFollowAsync(Athlete.Id, ct);

    }
}