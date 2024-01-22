using Microsoft.JSInterop;

namespace Tevling.Pages;

public partial class CreateChallenge : ComponentBase
{
    [Inject]
    IChallengeService ChallengeService { get; set; } = null!;

    [Inject]
    IJSRuntime JSRuntime { get; set; } = null!;

    private async Task OnSubmit(ChallengeFormModel challenge)
    {
        await ChallengeService.CreateChallengeAsync(challenge);
        await GoBack();
    }

    private Task OnCancel()
    {
        return GoBack();
    }

    private async Task GoBack()
    {
        await JSRuntime.InvokeVoidAsync("history.go", -1);
    }
}
