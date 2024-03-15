using Microsoft.JSInterop;

namespace Tevling.Components;

public partial class ChallengeCard : ComponentBase
{
    [Inject]
    IChallengeService ChallengeService { get; set; } = null!;
    [Inject]
    IJSRuntime JSRuntime { get; set; } = null!;

    [Parameter]
    public int AthleteId { get; set; }

    [Parameter]
    public Challenge? Challenge { get; set; }

    private ScoreBoard? ScoreBoard { get; set; }

    private bool ShowScoreBoard { get; set; }

    private bool? HasJoined =>
        Challenge?.Athletes?.Any(a => a.Id == AthleteId);

    private Athlete? Winner {get; set; }
    private bool DrawingWinner { get; set;}


    private string GetIconForMeasurement()
    {
        return Challenge?.Measurement switch
        {
            ChallengeMeasurement.Distance => "bi-arrow-right", 
            ChallengeMeasurement.Time => "bi-stopwatch-fill", 
            ChallengeMeasurement.Elevation => "bi-arrow-up", 
            _ => "bi-question-circle", 
        };
    }

    private async Task ToggleScoreBoard()
    {
        if (Challenge != null && (ShowScoreBoard = !ShowScoreBoard))
        {
            ScoreBoard = await ChallengeService.GetScoreBoardAsync(Challenge.Id);
        }
    }

    private async Task JoinChallenge()
    {
        if (Challenge != null)
        {
            Challenge = await ChallengeService.JoinChallengeAsync(AthleteId, Challenge.Id);
        }
    }

    private async Task LeaveChallenge()
    {
        if (Challenge != null)
        {
            Challenge = await ChallengeService.LeaveChallengeAsync(AthleteId, Challenge.Id);
        }
    }

    private async Task DeleteChallenge()
    {
        if (Challenge != null)
        {
            await ChallengeService.DeleteChallengeAsync(Challenge.Id);
        }
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
