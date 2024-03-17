using Microsoft.JSInterop;

namespace Tevling.Components;

public partial class ChallengeCard : ComponentBase
{
    [Inject] private IChallengeService ChallengeService { get; set; } = null!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = null!;
    [Inject] private IBrowserTime BrowserTime { get; set; } = null!;

    [Parameter] public int AthleteId { get; set; }
    [Parameter] public Challenge? Challenge { get; set; }

    private DateTimeOffset? CurrentBrowserTime { get; set; }
    private ScoreBoard? ScoreBoard { get; set; }

    private string MeasurementIcon
    {
        get
        {
            return Challenge?.Measurement switch
            {
                ChallengeMeasurement.Distance => "bi-arrow-right",
                ChallengeMeasurement.Time => "bi-stopwatch-fill",
                ChallengeMeasurement.Elevation => "bi-arrow-up",
                _ => "bi-question-circle"
            };
        }
    }

    private bool DrawingWinner { get; set; }
    private bool? HasJoined => Challenge?.Athletes?.Any(a => a.Id == AthleteId);
    private bool ShowScoreBoard { get; set; }
    private Athlete? Winner { get; set; }

    protected override async Task OnParametersSetAsync()
    {
        if (Challenge != null) CurrentBrowserTime = await BrowserTime.ConvertToLocal(DateTimeOffset.Now);
    }

    private async Task ToggleScoreBoard()
    {
        if (Challenge != null && (ShowScoreBoard = !ShowScoreBoard))
            ScoreBoard = await ChallengeService.GetScoreBoardAsync(Challenge.Id);
    }

    private async Task JoinChallenge()
    {
        if (Challenge != null) Challenge = await ChallengeService.JoinChallengeAsync(AthleteId, Challenge.Id);
    }

    private async Task LeaveChallenge()
    {
        if (Challenge != null) Challenge = await ChallengeService.LeaveChallengeAsync(AthleteId, Challenge.Id);
    }

    private async Task DeleteChallenge()
    {
        if (Challenge != null) await ChallengeService.DeleteChallengeAsync(Challenge.Id);
    }

    private async Task DrawWinner()
    {
        if (Challenge is not null)
        {
            DrawingWinner = true;
            await Task.Delay(TimeSpan.FromSeconds(3));
            Winner = await ChallengeService.DrawChallengeWinnerAsync(Challenge.Id);
            DrawingWinner = false;
        }
    }
}
