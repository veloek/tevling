namespace Tevling.Pages;

public partial class Profile : ComponentBase
{
    [Inject] private IAuthenticationService AuthenticationService { get; set; } = null!;
    [Inject] private IActivityService ActivityService { get; set; } = null!;
    [Inject] private IAthleteService AthleteService { get; set; } = null!;
    [Inject] private IBrowserTime BrowserTime { get; set; } = null!;

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
        CreatedTime = browserTime.ToString("dd'.'MM'.'yyyy");
    }

    private async Task Import()
    {
        if (Athlete.HasImportedActivities)
        {
            ImportResult = "Sorry, you can only import once.";
            return;
        }

        Importing = true;
        ImportResult = "Importing activities, please wait...";
        try
        {
            await ActivityService.ImportActivitiesForAthleteAsync(
                Athlete.Id,
                DateTimeOffset.Now - TimeSpan.FromDays(30));
            Athlete = await AthleteService.SetHasImportedActivities(Athlete.Id);
            ImportResult = "Import completed successfully";
        }
        catch (Exception ex)
        {
            ImportResult = $"Import failed with error: {ex.Message}";
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
}
