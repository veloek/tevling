using System.Globalization;
using Microsoft.JSInterop;
using Tevling.Strava;

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
        _startTime = DateTimeOffset.UtcNow.AddMonths(-3);

        ActivityFilter filter = new(_athlete.Id, false);
        _activities = await ActivityService.GetActivitiesAsync(filter);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            List<int> lastThreeMonths =
            [
                DateTimeOffset.Now.AddMonths(-2).Month,
                DateTimeOffset.Now.AddMonths(-1).Month,
                DateTimeOffset.Now.Month,
            ];

            Dictionary<string, float[]> distancesLastThreeMonths = _activities
                .Where(a => a.Details.StartDate >= DateTimeOffset.Now.AddMonths(-2))
                .GroupBy(a => a.Details.Type)
                .ToDictionary(
                    g => g.Key.ToString(),
                    g =>
                    {
                        var distances = g
                            .GroupBy(a => a.Details.StartDate.Month)
                            .ToDictionary(
                                g => g.Key,
                                g => g.Sum(a => a.Details.DistanceInMeters));

                        var now = DateTimeOffset.UtcNow;
                        return Enumerable.Range(-2, 3)
                            .Select(m => distances.GetValueOrDefault(now.AddMonths(m).Month, 0f))
                            .ToArray();
                    }
                );
            Dictionary<string, float[]> elevationLastThreeMonths = _activities
                .Where(a => a.Details.StartDate >= DateTimeOffset.Now.AddMonths(-2))
                .GroupBy(a => a.Details.Type)
                .ToDictionary(
                    g => g.Key.ToString(),
                    g =>
                    {
                        var elevation = g
                            .GroupBy(a => a.Details.StartDate.Month)
                            .ToDictionary(
                                g => g.Key,
                                g => g.Sum(a => a.Details.TotalElevationGain));

                        var now = DateTimeOffset.UtcNow;
                        return Enumerable.Range(-2, 3)
                            .Select(m => elevation.GetValueOrDefault(now.AddMonths(m).Month, 0f))
                            .ToArray();
                    }
                );

            if (distancesLastThreeMonths.Count > 0)
            {
                distancesLastThreeMonths["total"] =
                [
                    ..
                    distancesLastThreeMonths.Values.Aggregate((sum, next) => [.. sum.Zip(next, (a, b) => a + b)]),
                ];
            }
            
            if (elevationLastThreeMonths.Count > 0)
            {
                elevationLastThreeMonths["total"] =
                [
                    ..
                    elevationLastThreeMonths.Values.Aggregate((sum, next) => [.. sum.Zip(next, (a, b) => a + b)]),
                ];
            }


            // Call JavaScript function to draw chart
            await JS.InvokeVoidAsync(
                "drawChart",
                distancesLastThreeMonths,
                lastThreeMonths.Select(m => CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(m)).ToList(),
                "myChart",
                "Total Distance [m]");
            await JS.InvokeVoidAsync(
                "drawChart",
                elevationLastThreeMonths,
                lastThreeMonths.Select(m => CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(m)).ToList(),
                "myChart2",
                "Total Elevation [m]");
        }
    }
}
