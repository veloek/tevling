using Microsoft.Extensions.Localization;

namespace Tevling.Pages;

public partial class PublicProfile : ComponentBase
{
    [Inject] private IAuthenticationService AuthenticationService { get; set; } = null!;
    [Inject] private IStringLocalizer<Profile> Loc { get; set; } = null!;

    [Inject] private IAthleteService AthleteService { get; set; } = null!;

    [Inject] private IBrowserTime BrowserTime { get; set; } = null!;


    [Parameter] public int AthleteToViewId { get; set; }
    private Athlete Athlete { get; set; } = default!;
    private Athlete AthleteToView { get; set; } = default!;
    private string? CreatedTime;


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
