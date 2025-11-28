using Microsoft.Extensions.Localization;

namespace Tevling.Pages;

public partial class PublicProfile : ComponentBase
{
    private record Medals
    {
        public int First { get; set; }
        public int Second { get; set; }
        public int Third { get; set; }

        public string GetFirst() => First + " 🥇";
        public string GetSecond() => Second + " 🥈";
        public string GetThird() => Third + " 🥉";

        public void Reset()
        {
            First = 0;
            Second = 0;
            Third = 0;
        }
    }

    [Inject] private IAuthenticationService AuthenticationService { get; set; } = null!;
    [Inject] private IStringLocalizer<PublicProfile> Loc { get; set; } = null!;

    [Inject] private IAthleteService AthleteService { get; set; } = null!;

    [Inject] private IBrowserTime BrowserTime { get; set; } = null!;
    [Inject] private IChallengeService ChallengeService { get; set; } = null!;

    [Inject] private IActivityService ActivityService { get; set; } = null!;


    [Parameter] public int AthleteToViewId { get; set; }
    private Athlete Athlete { get; set; } = default!;
    private Athlete? AthleteToView { get; set; }
    private string? CreatedTime;
    private Dictionary<string, (string, string)> ActiveChallenges { get; } = [];
    private Medals AthleteMedals { get; } = new();
    private PublicProfileStats? AthleteStats { get; set; }


    protected override async Task OnInitializedAsync()
    {
        Athlete = await AuthenticationService.GetCurrentAthleteAsync();
    }

    protected override async Task OnParametersSetAsync()
    {
        Athlete? athleteToView = await AthleteService.GetAthleteByIdAsync(AthleteToViewId);
        if (athleteToView != null)
        {
            AthleteToView = athleteToView;
            DateTimeOffset browserTime = await BrowserTime.ConvertToLocal(AthleteToView!.Created);
            CreatedTime = browserTime.ToString("d");
            ActiveChallenges.Clear();
            AthleteMedals.Reset();
            await CountMedals();
            if (Athlete.IsFollowing(AthleteToViewId ) || Athlete.Id == AthleteToViewId)
            {
                await FetchActiveChallenges();
                await FetchStats();
            }
        }
    }

    private async Task FetchStats(CancellationToken ct = default)
    {
        AthleteStats = await ActivityService.GetPublicProfileStats(athleteId: AthleteToView!.Id, ct);
    }

    private async Task CountMedals(CancellationToken ct = default)
    {
        DateTimeOffset browserTime = await BrowserTime.ConvertToLocal(DateTimeOffset.Now, ct);
        ChallengeFilter filter = new(
            string.Empty,
            null,
            OnlyJoinedChallenges: true,
            IncludeOutdatedChallenges: true);
        Challenge[] challenges = await ChallengeService.GetChallengesAsync(AthleteToView!.Id, filter, null, ct);
        foreach (Challenge challenge in challenges)
        {
            if (challenge.End > browserTime) continue;
            ScoreBoard scoreBoard = await ChallengeService.GetScoreBoardAsync(challenge.Id, ct);

            AthleteScore? score = scoreBoard.Scores.FirstOrDefault(x => x.Name == AthleteToView.Name);
            if (score is null) continue;

            int placement = scoreBoard.Scores.ToList().IndexOf(score) + 1;
            switch (placement)
            {
                case 1:
                    AthleteMedals.First += 1;
                    break;
                case 2:
                    AthleteMedals.Second += 1;
                    break;
                case 3:
                    AthleteMedals.Third += 1;
                    break;
            }
        }
    }

    private async Task FetchActiveChallenges(CancellationToken ct = default)
    {
        DateTimeOffset browserTime = await BrowserTime.ConvertToLocal(DateTimeOffset.Now, ct);
        ChallengeFilter filter = new(
            string.Empty,
            null,
            OnlyJoinedChallenges: true,
            IncludeOutdatedChallenges: false);
        Challenge[] challenges = await ChallengeService.GetChallengesAsync(AthleteToView!.Id, filter, null, ct);

        foreach (Challenge challenge in challenges)
        {
            if (challenge.End <= browserTime) continue;

            ScoreBoard scoreBoard = await ChallengeService.GetScoreBoardAsync(challenge.Id, ct);

            AthleteScore? score = scoreBoard.Scores.FirstOrDefault(x => x.Name == AthleteToView.Name);
            if (score is null) continue;

            int placement = scoreBoard.Scores.ToList().IndexOf(score) + 1;
            if (placement is 0) continue;

            string placementString = GetOrdinal(placement);

            ActiveChallenges[challenge.Title] = (placementString, score.Score);
        }
    }

    private string GetOrdinal(int num)
    {
        if (num <= 0) return num.ToString();

        return num switch
        {
            1 => $"1{Loc["First"]} 🥇",
            2 => $"2{Loc["Second"]} 🥈",
            3 => $"3{Loc["Third"]} 🥉",
            _ => (num % 100) switch
            {
                11 or 12 or 13 => num + Loc["Nth"],
                _ => (num % 10) switch
                {
                    1 => num + Loc["First"],
                    2 => num + Loc["Second"],
                    3 => num + Loc["Third"],
                    _ => num + Loc["Nth"],
                },
            },
        };
    }

    private async Task ToggleFollowing(int followingId)
    {
        Athlete = await AthleteService.ToggleFollowingAsync(Athlete, followingId);

        if (Athlete.Id == AthleteToViewId)
        {
            AthleteToView = await AthleteService.GetAthleteByIdAsync(AthleteToViewId) ??
                throw new InvalidOperationException($"Athlete with ID {AthleteToViewId} not found");
        }

        await InvokeAsync(StateHasChanged);
    }

    private async Task RemoveFollower(int followerId)
    {
        Athlete = await AthleteService.RemoveFollowerAsync(Athlete, followerId);

        if (Athlete.Id == AthleteToViewId)
        {
            AthleteToView = await AthleteService.GetAthleteByIdAsync(AthleteToViewId) ??
                throw new InvalidOperationException($"Athlete with ID {AthleteToViewId} not found");
        }

        await InvokeAsync(StateHasChanged);
    }
}
