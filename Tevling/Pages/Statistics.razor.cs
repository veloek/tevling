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
    private int _numberOfMonthsToReview = 3;
    private ChallengeMeasurement _measurement = ChallengeMeasurement.Distance;

    protected override async Task OnInitializedAsync()
    {
        _athlete = await AuthenticationService.GetCurrentAthleteAsync();

        ActivityFilter filter = new(_athlete.Id, false, DateTimeOffset.Now.AddMonths(-12).ToFirstOfTheMonth());
        _activities = await ActivityService.GetActivitiesAsync(filter);
    }

    private Dictionary<string, float[]> GetAggregatedMeasurementData(Func<Activity, float> selector, int monthCount = 3)
    {
        DateTimeOffset now = DateTimeOffset.Now;

        Dictionary<string, float[]> aggregatedData = _activities
            .GroupBy(a => ActivityTypeExt.ToString(a.Details.Type))
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
            .Where(d => d.Value.Any(v => v > 0))
            .ToDictionary();

        return aggregatedData;
    }

    private string[] CreateMonthArray(int monthCount)
    {
        List<int> months = [];

        for (int i = 0; i < monthCount; i++)
        {
            months.Insert(0, DateTime.Now.AddMonths(-i).Month);
        }

        return [.. months.Select(CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName)];
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
            return;

        await DrawChart();
    }


    public async Task DrawChart()
    {
        _module = await Js.InvokeAsync<IJSObjectReference>("import", "./Pages/Statistics.razor.js");

        string[] months = CreateMonthArray(_numberOfMonthsToReview);

        Dictionary<string, float[]> distanceLastThreeMonths =
            GetAggregatedMeasurementData(a => (float)a.Details.DistanceInMeters / 1000, _numberOfMonthsToReview);
        Dictionary<string, float[]> elevationLastThreeMonths =
            GetAggregatedMeasurementData(a => a.Details.TotalElevationGain, _numberOfMonthsToReview);
        Dictionary<string, float[]> timeLastThreeMonths =
            GetAggregatedMeasurementData(a => (float)a.Details.MovingTimeInSeconds / 3600, _numberOfMonthsToReview);

        switch (_measurement)
        {
            case ChallengeMeasurement.Distance:
                await _module.InvokeVoidAsync(
                    "drawChart",
                    distanceLastThreeMonths,
                    months,
                    "TheChart",
                    "Total Distance [km]",
                    "km");
                break;
            case ChallengeMeasurement.Elevation:
                await _module.InvokeVoidAsync(
                    "drawChart",
                    elevationLastThreeMonths,
                    months,
                    "TheChart",
                    "Total Elevation [m]",
                    "m");
                break;
            case ChallengeMeasurement.Time:
                await _module.InvokeVoidAsync(
                    "drawChart",
                    timeLastThreeMonths,
                    months,
                    "TheChart",
                    "Total Time [h]",
                    "h");
                break;
            default:
                throw new Exception("Unknown challenge measurement");
        }
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
