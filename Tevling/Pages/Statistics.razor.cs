using System.Globalization;
using Microsoft.JSInterop;

namespace Tevling.Pages;

public partial class Statistics : ComponentBase
{
    [Inject] private IJSRuntime Js { get; set; } = null!;
    [Inject] private IAuthenticationService AuthenticationService { get; set; } = null!;
    [Inject] private IActivityService ActivityService { get; set; } = null!;

    private Athlete _athlete = null!;
    private Activity[] _activities = [];


    protected override async Task OnInitializedAsync()
    {
        _athlete = await AuthenticationService.GetCurrentAthleteAsync();

        ActivityFilter filter = new(_athlete.Id, false);
        _activities = await ActivityService.GetActivitiesAsync(filter);
    }

    private Dictionary<string, float[]> GetAggregatedData(Func<Activity, float> selector, int monthCount = 3)
    {
        DateTimeOffset now = DateTimeOffset.Now;
        Dictionary<string, float[]> aggregatedData = _activities
            .Where(a => a.Details.StartDate >= now.AddMonths(-monthCount + 1))
            .GroupBy(a => a.Details.Type)
            .ToDictionary(
                g => g.Key.ToString(),
                g =>
                {
                    Dictionary<int, float> monthlyTotals = g
                        .GroupBy(a => a.Details.StartDate.Month)
                        .ToDictionary(
                            g => g.Key,
                            g => g.Sum(selector));
                    return Enumerable.Range(-monthCount + 1, monthCount)
                        .Select(
                            m => monthlyTotals.GetValueOrDefault(now.AddMonths(m).Month, 0f)
                        )
                        .ToArray();
                });

        if (aggregatedData.Any())
        {
            aggregatedData["Total"] =
            [
                .. aggregatedData.Values.Aggregate((sum, next) => [.. sum.Zip(next, (a, b) => a + b)]),
            ];
        }

        return aggregatedData;
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

            Dictionary<string, float[]> distancesLastThreeMonths = GetAggregatedData(a => a.Details.DistanceInMeters);
            Dictionary<string, float[]> elevationLastThreeMonths = GetAggregatedData(a => a.Details.TotalElevationGain);
            Dictionary<string, float[]> timeLastThreeMonths =
                GetAggregatedData(a => (float)a.Details.MovingTimeInSeconds / 3600);


            await Js.InvokeVoidAsync(
                "drawChart",
                distancesLastThreeMonths,
                lastThreeMonths.Select(m => CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(m)).ToList(),
                "totalDistanceChart",
                "Total Distance [m]");
            await Js.InvokeVoidAsync(
                "drawChart",
                elevationLastThreeMonths,
                lastThreeMonths.Select(m => CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(m)).ToList(),
                "totalElevationChart",
                "Total Elevation [m]");
            await Js.InvokeVoidAsync(
                "drawChart",
                timeLastThreeMonths,
                lastThreeMonths.Select(m => CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(m)).ToList(),
                "totalTimeChart",
                "Total Time [h]");
        }
    }
}
