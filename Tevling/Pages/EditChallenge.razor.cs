using Microsoft.JSInterop;

namespace Tevling.Pages;

public partial class EditChallenge : ComponentBase
{
    [Inject] private IAuthenticationService AuthenticationService { get; set; } = null!;
    [Inject] private IChallengeService ChallengeService { get; set; } = null!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = null!;

    [Parameter] public int Id { get; set; }

    private Athlete _athlete = default!;
    private Challenge[] _challenges = [];
    private Challenge? Challenge { get; set; }

    protected override async Task OnInitializedAsync()
    {
        _athlete = await AuthenticationService.GetCurrentAthleteAsync();
    }

    protected override async Task OnParametersSetAsync()
    {
        Challenge? challenge = await ChallengeService.GetChallengeByIdAsync(Id);

        if (challenge?.CreatedById == _athlete.Id) Challenge = challenge;
    }

    private async Task OnSubmit(ChallengeFormModel challenge)
    {
        await ChallengeService.UpdateChallengeAsync(Challenge!.Id, challenge);
        await GoBack();
    }

    private async Task OnCancel()
    {
        await GoBack();
    }

    private async Task GoBack()
    {
        await JSRuntime.InvokeVoidAsync("history.go", -1);
    }
}
