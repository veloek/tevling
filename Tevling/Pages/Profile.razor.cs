using Microsoft.Extensions.Localization;

namespace Tevling.Pages;

public partial class Profile : ComponentBase
{
    [Inject] private IAuthenticationService AuthenticationService { get; set; } = null!;
    [Inject] private IActivityService ActivityService { get; set; } = null!;
    [Inject] private IAthleteService AthleteService { get; set; } = null!;
    [Inject] private IBrowserTime BrowserTime { get; set; } = null!;
    [Inject] private IStringLocalizer<Profile> Loc { get; set; } = null!;
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;

    private string? CreatedTime;
    private bool Importing { get; set; }
    private string? ImportResult { get; set; }
    private Athlete Athlete { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        Athlete = await AuthenticationService.GetCurrentAthleteAsync();
    }

    protected override async Task OnParametersSetAsync()
    {
        DateTimeOffset browserTime = await BrowserTime.ConvertToLocal(Athlete.Created);
        CreatedTime = browserTime.ToString("d");
    }

    private async Task Import()
    {
        if (Athlete.HasImportedActivities)
        {
            ImportResult = Loc["SorryOnlyOnce"];
            return;
        }

        Importing = true;
        ImportResult = Loc["ImportingActivities"];
        try
        {
            await ActivityService.ImportActivitiesForAthleteAsync(
                Athlete.Id,
                DateTimeOffset.Now - TimeSpan.FromDays(30));
            Athlete = await AthleteService.SetHasImportedActivities(Athlete.Id);
            ImportResult = Loc["ImportSuccessful"];
        }
        catch (Exception ex)
        {
            ImportResult = string.Format(Loc["ImportFailed"], ex.Message);
        }
        finally
        {
            Importing = false;
        }
    }

    private async Task ToggleFollowing(int followingId)
    {
        Athlete = await AthleteService.ToggleFollowingAsync(Athlete, followingId);
        await InvokeAsync(StateHasChanged);
    }

    private async Task RemoveFollower(int followerId)
    {
        Athlete = await AthleteService.RemoveFollowerAsync(Athlete, followerId);
        await InvokeAsync(StateHasChanged);
    }

    private async Task AcceptFollower(int followerId)
    {
        Athlete = await AthleteService.AcceptFollowerAsync(Athlete, followerId);
        await InvokeAsync(StateHasChanged);
    }

    private async Task DeclineFollower(int followerId)
    {
        Athlete = await AthleteService.DeclineFollowerAsync(Athlete, followerId);
        await InvokeAsync(StateHasChanged);
    }

    private void SignOut()
    {
        NavigationManager.NavigateTo("/auth/logout", forceLoad: true);
    }

    private void Deauthorize()
    {
        NavigationManager.NavigateTo("/auth/logout?deauthorize=true", forceLoad: true);
    }
}
