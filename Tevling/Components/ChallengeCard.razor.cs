using System.Globalization;
using Microsoft.JSInterop;

namespace Tevling.Components;

public partial class ChallengeCard : ComponentBase
{
    [Inject] private IChallengeService ChallengeService { get; set; } = null!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = null!;
    [Inject] private IBrowserTime BrowserTime { get; set; } = null!;
    [Inject] private IConfiguration Configuration { get; set; } = null!;

    [Parameter] public int AthleteId { get; set; }
    [Parameter] public Challenge? Challenge { get; set; }

    private DateTimeOffset? CurrentBrowserTime { get; set; }
    private ScoreBoard? ScoreBoard { get; set; }
    private bool IsAdmin => Configuration.GetSection("AdminIds").Get<int[]>()?.Contains(AthleteId) ?? false;

    private string MeasurementIcon
    {
        get
        {
            return Challenge?.Measurement switch
            {
                ChallengeMeasurement.Distance => "bi-arrow-right",
                ChallengeMeasurement.Time => "bi-stopwatch-fill",
                ChallengeMeasurement.Elevation => "bi-arrow-up",
                ChallengeMeasurement.Calories => "bi-fire",
                _ => "bi-question-circle",
            };
        }
    }

    private bool DrawingWinner { get; set; }
    private bool? HasJoined => Challenge?.Athletes?.Any(a => a.Id == AthleteId);
    private bool ShowScoreBoard { get; set; }
    private Athlete? Winner { get; set; }
    private string? GoalDisplay => Challenge?.IndividualGoal is null
        ? null
        : Challenge.Measurement switch
        {
            ChallengeMeasurement.Distance => $"{Challenge.IndividualGoal:0.##} km",
            ChallengeMeasurement.Time => TimeSpan.FromHours(Challenge.IndividualGoal.Value).ToString("g"),
            ChallengeMeasurement.Elevation => $"{Challenge.IndividualGoal:0.##} m",
            ChallengeMeasurement.Calories => $"{Challenge.IndividualGoal:0.##} kcal",
            _ => Challenge.IndividualGoal.Value.ToString("0.##"),
        };
    private bool HasGoal => Challenge?.IndividualGoal is > 0;

    private bool HasReachedGoal(float scoreValue)
    {
        return HasGoal && scoreValue >= Challenge!.IndividualGoal!.Value;
    }

    private string RemainingDisplay(float scoreValue)
    {
        if (!HasGoal) return string.Empty;

        float remaining = Math.Max(Challenge!.IndividualGoal!.Value - scoreValue, 0);

        return Challenge.Measurement switch
        {
            ChallengeMeasurement.Distance => $"{remaining:0.##} km",
            ChallengeMeasurement.Time => TimeSpan.FromHours(remaining).ToString("g"),
            ChallengeMeasurement.Elevation => $"{remaining:0.##} m",
            ChallengeMeasurement.Calories => $"{remaining:0.##} kcal",
            _ => remaining.ToString("0.##"),
        };
    }

    private string ProgressPercent(float scoreValue)
    {
        if (!HasGoal) return "0%";

        float percent = Math.Clamp(scoreValue / Challenge!.IndividualGoal!.Value * 100f, 0f, 100f);
        return string.Create(CultureInfo.InvariantCulture, $"{percent:0.#}%");
    }

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

    private async Task DrawStats()
    {
        List<string> users = [.. ScoreBoard!.Scores.Select(score => score.Name)];
        List<float> data = [.. ScoreBoard!.Scores.Select(score => score.ScoreValue)];

        await JSRuntime.InvokeVoidAsync(
            "DrawChallengeStats",
            "distribution-" + Challenge!.Id,
            data,
            users,
            Challenge.Measurement.ToString());
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

    private async Task RedrawWinner()
    {
        if (Challenge is null) return;

        DrawingWinner = true;
        await ChallengeService.ClearChallengeWinnerAsync(Challenge.Id);
        await this.DrawWinner();
    }

    private void OpenDrawWinnerModal()
    {
        Winner = Challenge?.Winner;
    }
}
