using Microsoft.Extensions.Localization;
using Microsoft.JSInterop;
using System.Globalization;

namespace Tevling.Pages;

public partial class Statistics : ComponentBase, IAsyncDisposable
{
    private record Stats
    {
        public required string Type { get; init; }
        public required float[] LastTimePeriodAggregate { get; init; }
        public double LastTimePeriodAverage => Math.Round(LastTimePeriodAggregate.Average(), 1);
        public double LastTimePeriodTotal => Math.Round(LastTimePeriodAggregate.Sum(), 1);

        public float ThisTimePeriod => LastTimePeriodAggregate[^1];

        public double CurrentMonthComparedToAverage()
        {
            if (LastTimePeriodAverage == 0)
            {
                return 100;
            }

            double difference =
                100 * (Math.Round(ThisTimePeriod, 1) / LastTimePeriodAverage);
            if (difference > 100)
            {
                return difference - 100;
            }

            return 100 - difference;
        }

        public string IncreaseVsDecrease(string increase, string decrease)
        {
            if (LastTimePeriodAverage == 0)
            {
                return "<span style='color:green'>" + increase + "</span>";
            }

            return Math.Round(ThisTimePeriod, 1) / LastTimePeriodAverage > 1
                ? "<span style='color:green'>" + increase + "</span>"
                : "<span style='color:red'>" + decrease + "</span>";
        }
    }


    [Inject] private IJSRuntime Js { get; set; } = null!;
    [Inject] private IAuthenticationService AuthenticationService { get; set; } = null!;
    [Inject] private IActivityService ActivityService { get; set; } = null!;
    [Inject] private IStringLocalizer<Statistics> Loc { get; set; } = null!;
    [Inject] private ActivityTypeTranslator ActivityTypeTranslator { get; set; } = null!;

    private Athlete _athlete = null!;
    private Activity[] _activities = [];
    private IJSObjectReference? _module;
    private IJSObjectReference? _resizeHandler;

    private TimePeriod TimePeriod { get; set; } = TimePeriod.Months;
    private int NumberOfPeriodsToReview { get; set; } = 3;
    private ChallengeMeasurement Measurement { get; set; } = ChallengeMeasurement.Distance;
    private IReadOnlyList<Stats> Distances { get; set; } = [];
    private IReadOnlyList<Stats> Elevations { get; set; } = [];
    private IReadOnlyList<Stats> Durations { get; set; } = [];
    private IReadOnlyList<Stats> Calories { get; set; } = [];

    protected override async Task OnInitializedAsync()
    {
        _athlete = await AuthenticationService.GetCurrentAthleteAsync();
        await UpdateMeasurementData();
    }

    private List<Stats> GetAggregatedMeasurementData(Func<Activity, float> selector, int periodCount = 3)
    {
        DateTimeOffset now = DateTimeOffset.Now;

        return
        [
            .. _activities
                .GroupBy(a => ActivityTypeTranslator.Translate(a.Details.Type))
                .ToDictionary(
                    g => g.Key.ToString(),
                    g => Enumerable.Range(-periodCount + 1, periodCount)
                        .Select(m =>
                        {
                            switch (TimePeriod)
                            {
                                case TimePeriod.Months:
                                    int month = now.AddMonths(m).Month;
                                    int year = now.AddMonths(m).Year;
                                    return g
                                        .Where(a => a.Details.StartDate.Month == month &&
                                            a.Details.StartDate.Year == year)
                                        .Sum(selector);
                                case TimePeriod.Weeks:
                                    int week = ISOWeek.GetWeekOfYear(now.AddDays(m * 7).DateTime);
                                    int weekYear = now.AddDays(m * 7).Year;
                                    return g
                                        .Where(a => ISOWeek.GetWeekOfYear(a.Details.StartDate.DateTime) == week &&
                                            a.Details.StartDate.Year == weekYear)
                                        .Sum(selector);
                                default:
                                    throw new Exception("Unknown time period");
                            }
                        })
                        .ToArray()
                )
                .Where(d => d.Value.Any(v => v > 0))
                .Select(kvp => new Stats
                    {
                        Type = kvp.Key,
                        LastTimePeriodAggregate = kvp.Value,
                    }
                ),
        ];
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
                .Reverse(),
        ];
    }

    private static string[] CreateWeekArray(int weekCount)
    {
        return
        [
            .. Enumerable.Range(0, weekCount)
                .Select(i =>
                {
                    DateTime week = DateTime.Now.AddDays(-i * 7);

                    return ISOWeek.GetWeekOfYear(week).ToString();
                })
                .Reverse(),
        ];
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
            return;

        await DrawChart();
    }

    private static DateTimeOffset GetFirstOfTheWeek(DateTimeOffset date)
    {
        DayOfWeek day = date.DayOfWeek;
        // IsoWeeks Starts on monday, but DayOfWeek starts on sunday
        return day == DayOfWeek.Sunday ? date.AddDays(-6) : date.AddDays(-(int)day + 1);
    }

    private async Task UpdateMeasurementData()
    {
        DateTimeOffset startDate = TimePeriod switch
        {
            TimePeriod.Months => DateTimeOffset.Now.AddMonths(-NumberOfPeriodsToReview + 1).ToFirstOfTheMonth(),
            TimePeriod.Weeks => GetFirstOfTheWeek(DateTimeOffset.Now.AddDays(-NumberOfPeriodsToReview * 7)),
            _ => throw new Exception("Unknown time period"),
        };

        ActivityFilter filter = new(
            _athlete.Id,
            false,
            startDate);
        _activities = await ActivityService.GetActivitiesAsync(filter);

        Distances =
        [
            .. GetAggregatedMeasurementData(
                a => a.Details.DistanceInMeters / 1000,
                NumberOfPeriodsToReview),
        ];
        Elevations =
        [
            .. GetAggregatedMeasurementData(a => a.Details.TotalElevationGain, NumberOfPeriodsToReview),
        ];
        Durations =
        [
            .. GetAggregatedMeasurementData(
                a => (float)a.Details.MovingTimeInSeconds / 3600,
                NumberOfPeriodsToReview),
        ];
        Calories =
        [
            .. GetAggregatedMeasurementData(a => a.Details.Calories, NumberOfPeriodsToReview),
        ];
    }

    private Dictionary<string, float[]> AggregateTotals(IReadOnlyList<Stats> stats)
    {
        float[] totalStats = new float[NumberOfPeriodsToReview];
        foreach (Stats stat in stats)
        {
            for (int i = 0; i < stat.LastTimePeriodAggregate.Length; i++)
            {
                totalStats[i] += stat.LastTimePeriodAggregate[i];
            }
        }

        return new Dictionary<string, float[]>
        {
            {
                "Total", totalStats
            },
        };
    }

    private async Task DrawChart()
    {
        _module ??= await Js.InvokeAsync<IJSObjectReference>("import", "./Pages/Statistics.razor.js");

        string[] timePeriodArray = TimePeriod switch
        {
            TimePeriod.Months => CreateMonthArray(NumberOfPeriodsToReview),
            TimePeriod.Weeks => CreateWeekArray(NumberOfPeriodsToReview),
            _ => throw new Exception("Unknown time period"),
        };

        await UpdateMeasurementData();

        bool isMobile = await _module.InvokeAsync<bool>("isMobile");
        switch (Measurement)
        {
            case ChallengeMeasurement.Distance:
                await _module.InvokeVoidAsync(
                    "drawChart",
                    isMobile
                        ? AggregateTotals(Distances)
                        : Distances.ToDictionary(stat => stat.Type, stat => stat.LastTimePeriodAggregate),
                    timePeriodArray,
                    "TheChart",
                    Loc["TotalDistance"] + " [km]",
                    "km");
                break;
            case ChallengeMeasurement.Elevation:
                await _module.InvokeVoidAsync(
                    "drawChart",
                    isMobile
                        ? AggregateTotals(Elevations)
                        : Elevations.ToDictionary(stat => stat.Type, stat => stat.LastTimePeriodAggregate),
                    timePeriodArray,
                    "TheChart",
                    Loc["TotalElevation"] + " [m]",
                    "m");
                break;
            case ChallengeMeasurement.Time:
                await _module.InvokeVoidAsync(
                    "drawChart",
                    isMobile
                        ? AggregateTotals(Durations)
                        : Durations.ToDictionary(stat => stat.Type, stat => stat.LastTimePeriodAggregate),
                    timePeriodArray,
                    "TheChart",
                    Loc["TotalTime"] + " [h]",
                    "h");
                break;
            case ChallengeMeasurement.Calories:
                await _module.InvokeVoidAsync(
                    "drawChart",
                    isMobile
                        ? AggregateTotals(Calories)
                        : Calories.ToDictionary(stat => stat.Type, stat => stat.LastTimePeriodAggregate),
                    timePeriodArray,
                    "TheChart",
                    Loc["TotalCalories"] + " [kcal]",
                    "kcal");
                break;
            default:
                throw new Exception("Unknown challenge measurement");
        }

       _resizeHandler ??= await _module.InvokeAsync<IJSObjectReference>("enableCanvasResize", "TheChart");
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_resizeHandler != null)
            {
                await _resizeHandler.InvokeVoidAsync("dispose");
                await _resizeHandler.DisposeAsync();
                _resizeHandler = null;
            }

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
