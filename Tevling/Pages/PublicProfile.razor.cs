using Microsoft.Extensions.Localization;

namespace Tevling.Pages;

public partial class PublicProfile : ComponentBase
{
    [Inject] private IAuthenticationService AuthenticationService { get; set; } = null!;
    [Inject] private IStringLocalizer<PublicProfile> Loc { get; set; } = null!;

    [Inject] private IAthleteService AthleteService { get; set; } = null!;

    [Inject] private IBrowserTime BrowserTime { get; set; } = null!;
    [Inject] private IChallengeService ChallengeService { get; set; } = null!;
    


    [Parameter] public int AthleteToViewId { get; set; }
    private Athlete Athlete { get; set; } = default!;
    private Athlete AthleteToView { get; set; } = default!;
    private string? CreatedTime;
    private Dictionary<Challenge, (string, string)> ActiveChallenges { get; } = [];


    protected override async Task OnInitializedAsync()
    {
        Athlete = await AuthenticationService.GetCurrentAthleteAsync();
        AthleteToView = await AthleteService.GetAthleteByIdAsync(AthleteToViewId) ??
            throw new InvalidOperationException($"Athlete with ID {AthleteToViewId} not found");
    }

    protected override async Task OnParametersSetAsync()
    {
        AthleteToView = await AthleteService.GetAthleteByIdAsync(AthleteToViewId) ??
            throw new InvalidOperationException($"Athlete with ID {AthleteToViewId} not found");
        DateTimeOffset browserTime = await BrowserTime.ConvertToLocal(AthleteToView.Created);
        CreatedTime = browserTime.ToString("d");
        await FetchActiveChallenges();
    }
    
    private async Task FetchActiveChallenges(CancellationToken ct = default)
    {
        DateTimeOffset browserTime = await BrowserTime.ConvertToLocal(DateTimeOffset.Now, ct);
        ChallengeFilter filter = new(
            string.Empty,
            null,
            OnlyJoinedChallenges: true,
            IncludeOutdatedChallenges: false);
        Challenge[] challenges = await ChallengeService.GetChallengesAsync(AthleteToView.Id, filter, null, ct);

        foreach (Challenge challenge in challenges)
        {
            if (challenge.End <= browserTime) continue;

            ScoreBoard scoreBoard = await ChallengeService.GetScoreBoardAsync(challenge.Id, ct);

            AthleteScore? score = scoreBoard.Scores.FirstOrDefault(x => x.Name == AthleteToView.Name);
            if (score is null) continue;

            int placement = scoreBoard.Scores.ToList().IndexOf(score) + 1;
            if (placement is 0) continue;

            string placementString = GetOrdinal(placement);

            ActiveChallenges[challenge] = (placementString, score.Score);
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
