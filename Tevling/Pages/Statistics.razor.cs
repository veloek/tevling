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

    private int NumberOfMonthsToReview { get; set; } = 3;
    private ChallengeMeasurement Measurement { get; set; } = ChallengeMeasurement.Distance;
    private Dictionary<string, float[]> Distances { get; set; } = [];
    private Dictionary<string, float[]> Elevations { get; set; } = [];
    private Dictionary<string, float[]> Durations { get; set; } = [];

    protected override async Task OnInitializedAsync()
    {
        _athlete = await AuthenticationService.GetCurrentAthleteAsync();
        UpdateMeasurementData();
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
                        int year = now.AddMonths(m).Year;
                        return g
                            .Where(a => a.Details.StartDate.Month == month && a.Details.StartDate.Year == year)
                            .Sum(selector);
                    })
                    .ToArray()
            )
            .Where(d => d.Value.Any(v => v > 0))
            .ToDictionary();

        return aggregatedData;
    }

    private static string[] CreateMonthArray(int monthCount)
    {
        return
        [
            .. Enumerable.Range(0, monthCount)
                .Select(i =>
                {
                    DateTime month = DateTime.Now.AddMonths(-i);
                    return month.ToString(month.Month == 1 ? "MMMM-yy" : "MMMM");
                })
                .Reverse()
        ];
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
            return;

        await DrawChart();
    }

    private async Task UpdateMeasurementData()
    {
        ActivityFilter filter = new(
            _athlete.Id,
            false,
            DateTimeOffset.Now.AddMonths(-NumberOfMonthsToReview + 1).ToFirstOfTheMonth());
        _activities = await ActivityService.GetActivitiesAsync(filter);
        
        Distances = GetAggregatedMeasurementData(
            a => a.Details.DistanceInMeters / 1000,
            NumberOfMonthsToReview);
        Elevations = GetAggregatedMeasurementData(a => a.Details.TotalElevationGain, NumberOfMonthsToReview);
        Durations = GetAggregatedMeasurementData(
            a => (float)a.Details.MovingTimeInSeconds / 3600,
            NumberOfMonthsToReview);
    }

    private async Task DrawChart()
    {
        _module = await Js.InvokeAsync<IJSObjectReference>("import", "./Pages/Statistics.razor.js");

        string[] months = CreateMonthArray(NumberOfMonthsToReview);

        UpdateMeasurementData();

        switch (Measurement)
        {
            case ChallengeMeasurement.Distance:
                await _module.InvokeVoidAsync(
                    "drawChart",
                    Distances,
                    months,
                    "TheChart",
                    "Total Distance [km]",
                    "km");
                break;
            case ChallengeMeasurement.Elevation:
                await _module.InvokeVoidAsync(
                    "drawChart",
                    Elevations,
                    months,
                    "TheChart",
                    "Total Elevation [m]",
                    "m");
                break;
            case ChallengeMeasurement.Time:
                await _module.InvokeVoidAsync(
                    "drawChart",
                    Durations,
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
