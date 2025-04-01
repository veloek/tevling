using System.Globalization;
using Microsoft.JSInterop;

namespace Tevling.Pages;

public partial class Statistics : ComponentBase
{
    [Inject] IJSRuntime JS { get; set; } = default!;
    [Inject] private IAuthenticationService AuthenticationService { get; set; } = null!;
    [Inject] private IActivityService ActivityService { get; set; } = null!;


    private Athlete _athlete { get; set; } = default!;
    private Activity[] _activities { get; set; } = [];

    private DateTimeOffset _startTime;
    private DateTimeOffset _endTime;


    protected override async Task OnInitializedAsync()
    {
        _athlete = await AuthenticationService.GetCurrentAthleteAsync();
        _endTime = DateTimeOffset.UtcNow;
        _startTime = DateTimeOffset.UtcNow - TimeSpan.FromDays(30 * 3);

        ActivityFilter filter = new(_athlete.Id, false);
        _activities = await ActivityService.GetActivitiesAsync(filter);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            Dictionary<string, float> lastThreeMonths = _activities.Where(
                    a => a.Details.StartDate >= DateTimeOffset.Now.AddMonths(-3))
                .GroupBy(a => a.Details.StartDate.ToString("MMMM", CultureInfo.InvariantCulture))
                .ToDictionary(g => g.Key, g => g.Sum(b => b.Details.DistanceInMeters));

            // Call JavaScript function to draw chart
            await JS.InvokeVoidAsync("drawChart", lastThreeMonths.Values.Reverse(), lastThreeMonths.Keys.Reverse());
        }
    }
}
