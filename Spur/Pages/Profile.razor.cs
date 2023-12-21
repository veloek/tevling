namespace Spur.Pages;

public partial class Profile : ComponentBase
{
    [Inject]
    IAuthenticationService AuthenticationService { get; set; } = null!;

    [Inject]
    IActivityService ActivityService { get; set; } = null!;

    [Inject]
    IAthleteService AthleteService { get; set; } = null!;

    [Inject]
    IBrowserTime BrowserTime { get; set; } = null!;

    private Athlete Athlete { get; set; } = default!;
    private string? CreatedTime;
    private bool Importing { get; set; }
    private string? ImportResult { get; set; }

    protected override async Task OnInitializedAsync()
    {
        Athlete = await AuthenticationService.GetCurrentAthleteAsync();
    }

    protected override async Task OnParametersSetAsync()
    {
        DateTimeOffset browserTime = await BrowserTime.ConvertToLocal(Athlete.Created);
        CreatedTime = browserTime.ToString("yyyy'-'MM'-'dd");
    }

    public async Task Import()
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
            await ActivityService.ImportActivitiesForAthlete(Athlete.Id);
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
}
