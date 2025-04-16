using System.Globalization;
using Microsoft.JSInterop;
using Tevling.Strava;

namespace Tevling.Pages;

public partial class Statistics : ComponentBase
{
    [Inject] IJSRuntime JS { get; set; } = default!;
    [Inject] private IAuthenticationService AuthenticationService { get; set; } = null!;
    [Inject] private IActivityService ActivityService { get; set; } = null!;

    private Athlete Athlete { get; set; } = default!;
    private Activity[] Activities { get; set; } = [];


    protected override async Task OnInitializedAsync()
    {
        Athlete = await AuthenticationService.GetCurrentAthleteAsync();

        ActivityFilter filter = new(Athlete.Id, false);
        Activities = await ActivityService.GetActivitiesAsync(filter);
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

            Dictionary<string, float[]> distancesLastThreeMonths = Activities
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
            Dictionary<string, float[]> elevationLastThreeMonths = Activities
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

            Dictionary<string, float[]> timeLastThreeMonths = Activities
                .Where(a => a.Details.StartDate >= DateTimeOffset.Now.AddMonths(-2))
                .GroupBy(a => a.Details.Type)
                .ToDictionary(
                    g => g.Key.ToString(),
                    g =>
                    {
                        var movingTime = g
                            .GroupBy(a => a.Details.StartDate.Month)
                            .ToDictionary(
                                g => g.Key,
                                g => (float)g.Sum(a => a.Details.MovingTimeInSeconds) / 3600);

                        var now = DateTimeOffset.UtcNow;
                        return Enumerable.Range(-2, 3)
                            .Select(m => movingTime.GetValueOrDefault(now.AddMonths(m).Month, 0))
                            .ToArray();
                    }
                );

            if (distancesLastThreeMonths.Count > 0)
            {
                distancesLastThreeMonths["Total"] =
                [
                    ..
                    distancesLastThreeMonths.Values.Aggregate((sum, next) => [.. sum.Zip(next, (a, b) => a + b)]),
                ];
            }

            if (elevationLastThreeMonths.Count > 0)
            {
                elevationLastThreeMonths["Total"] =
                [
                    ..
                    elevationLastThreeMonths.Values.Aggregate((sum, next) => [.. sum.Zip(next, (a, b) => a + b)]),
                ];
            }

            if (timeLastThreeMonths.Count > 0)
            {
                timeLastThreeMonths["Total"] =
                [
                    ..
                    timeLastThreeMonths.Values.Aggregate((sum, next) => [.. sum.Zip(next, (a, b) => a + b)]),
                ];
            }


            await JS.InvokeVoidAsync(
                "drawChart",
                distancesLastThreeMonths,
                lastThreeMonths.Select(m => CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(m)).ToList(),
                "totalDistanceChart",
                "Total Distance [m]");
            await JS.InvokeVoidAsync(
                "drawChart",
                elevationLastThreeMonths,
                lastThreeMonths.Select(m => CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(m)).ToList(),
                "totalElevationChart",
                "Total Elevation [m]");
            await JS.InvokeVoidAsync(
                "drawChart",
                timeLastThreeMonths,
                lastThreeMonths.Select(m => CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(m)).ToList(),
                "totalTimeChart",
                "Total Time [h]");
        }
    }
}
