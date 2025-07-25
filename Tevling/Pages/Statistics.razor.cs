using System.Globalization;
using Microsoft.JSInterop;

namespace Tevling.Pages;

public partial class Statistics : ComponentBase, IAsyncDisposable
{
    [Inject] private IJSRuntime Js { get; set; } = null!;
    [Inject] private IAuthenticationService AuthenticationService { get; set; } = null!;
    [Inject] private IActivityService ActivityService { get; set; } = null!;

    private Athlete _athlete = null!;
    private Activity[] _activities = [];
    private IJSObjectReference? _module;

    protected override async Task OnInitializedAsync()
    {
        _athlete = await AuthenticationService.GetCurrentAthleteAsync();

        ActivityFilter filter = new(_athlete.Id, false, DateTimeOffset.Now.AddMonths(-2).ToFirstOfTheMonth());
        _activities = await ActivityService.GetActivitiesAsync(filter);
    }

    private Dictionary<string, float[]> GetAggregatedMeasurementData(Func<Activity, float> selector, int monthCount = 3)
    {
        DateTimeOffset now = DateTimeOffset.Now;

        Dictionary<string, float[]> aggregatedData = _activities
            .GroupBy(a => a.Details.Type)
            .ToDictionary(
                g => g.Key.ToString(),
                g => Enumerable.Range(-monthCount + 1, monthCount)
                    .Select(m =>
                    {
                        int month = now.AddMonths(m).Month;
                        return g
                            .Where(a => a.Details.StartDate.Month == month)
                            .Sum(selector);
                    })
                    .ToArray()
            )
            .Where(d => d.Value.Length > 0)
            .ToDictionary();

        // if (aggregatedData.Count != 0)
        // {
        //     aggregatedData["Total"] =
        //     [
        //         .. aggregatedData.Values.Aggregate((sum, next) => [.. sum.Zip(next, (a, b) => a + b)]),
        //     ];
        // }

        return aggregatedData;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
            return;

        _module = await Js.InvokeAsync<IJSObjectReference>("import", "./Pages/Statistics.razor.js");

        string[] lastThreeMonths = new int[]
            {
                DateTimeOffset.Now.AddMonths(-2).Month,
                DateTimeOffset.Now.AddMonths(-1).Month,
                DateTimeOffset.Now.Month,
            }
            .Select(CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName)
            .ToArray();

        Dictionary<string, float[]> distanceLastThreeMonths =
            GetAggregatedMeasurementData(a => (float)a.Details.DistanceInMeters / 1000);
        Dictionary<string, float[]> elevationLastThreeMonths =
            GetAggregatedMeasurementData(a => a.Details.TotalElevationGain);
        Dictionary<string, float[]> timeLastThreeMonths =
            GetAggregatedMeasurementData(a => (float)a.Details.MovingTimeInSeconds / 3600);

        await _module.InvokeVoidAsync(
            "drawChart",
            distanceLastThreeMonths,
            lastThreeMonths,
            "totalDistanceChart",
            "Total Distance [km]");
        await _module.InvokeVoidAsync(
            "drawChart",
            elevationLastThreeMonths,
            lastThreeMonths,
            "totalElevationChart",
            "Total Elevation [m]");
        await _module.InvokeVoidAsync(
            "drawChart",
            timeLastThreeMonths,
            lastThreeMonths,
            "totalTimeChart",
            "Total Time [h]");
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_module != null)
            {
                await _module.DisposeAsync();
                _module = null;
            }
        }
        catch (JSDisconnectedException)
        {
            // Ignore, happens during page reload.
        }
    }
}
