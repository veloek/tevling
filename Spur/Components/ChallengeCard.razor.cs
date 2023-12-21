using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Spur.Model;
using Spur.Services;

namespace Spur.Components;

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

    private string HeaderStyle =>
        HasJoined == true || Challenge?.CreatedById == AthleteId
            ? "border-primary"
            : "border-light";

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
        bool confirmed = await JSRuntime.InvokeAsync<bool>("confirm", "Are you sure you want to delete the challenge?");
        if (confirmed)
        {
            if (Challenge != null)
            {
                await ChallengeService.DeleteChallengeAsync(Challenge.Id);
            }
        }
    }
}
