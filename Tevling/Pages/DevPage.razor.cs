namespace Tevling.Pages;

public partial class DevPage : ComponentBase
{
    [Inject] private IAuthenticationService AuthenticationService { get; set; } = null!;

    private Athlete Athlete { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        Athlete = await AuthenticationService.GetCurrentAthleteAsync();
    }
}
