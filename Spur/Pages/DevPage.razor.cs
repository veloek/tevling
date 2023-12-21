using Microsoft.AspNetCore.Components;
using Spur.Model;
using Spur.Services;

namespace Spur.Pages;

public partial class DevPage : ComponentBase
{
    [Inject]
    IAuthenticationService AuthenticationService { get; set; } = null!;

    private Athlete Athlete { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        Athlete = await AuthenticationService.GetCurrentAthleteAsync();
    }
}
