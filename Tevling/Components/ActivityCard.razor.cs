using Microsoft.Extensions.Localization;

namespace Tevling.Components;

public partial class ActivityCard : ComponentBase
{
    [Inject] private IBrowserTime BrowserTime { get; set; } = null!;
    [Inject] private IStringLocalizer<ActivityCard> Loc { get; set; } = null!;

    [Parameter] public Activity? Activity { get; set; }

    private string? ActivityTime { get; set; }

    private string Stats
    {
        get
        {
            if (Activity is null) return string.Empty;
            if (Activity.Details.DistanceInMeters > 0)
            {
                float distanceInKm = Activity.Details.DistanceInMeters / 1000;
                return $"{Loc["Distance"]}: {distanceInKm:F1} km";
            }

            if (Activity.Details.TotalElevationGain > 0)
            {
                return $"{Loc["Elevation"]}: {Activity.Details.TotalElevationGain} m";
            }

            return Activity.Details.Calories > 0 ? $"{Loc["Calories"]}: {Activity.Details.Calories} kcal" : string.Empty;
        }
    }

    private string Time
    {
        get
        {
            if (Activity?.Details.MovingTimeInSeconds is null or <= 0) return string.Empty;

            TimeSpan timeSpan = TimeSpan.FromSeconds(Activity.Details.MovingTimeInSeconds);
            string formattedTime = "";

            if (timeSpan.Hours > 0)
                formattedTime += $"{timeSpan.Hours}{Loc["Hours"]} ";

            if (timeSpan.Minutes > 0 || timeSpan.Hours > 0) // Include minutes if there are any hours
                formattedTime += $"{timeSpan.Minutes}{Loc["Minutes"]} ";

            formattedTime += $"{timeSpan.Seconds}{Loc["Seconds"]}";

            return $"{Loc["Time"]}: {formattedTime}";
        }
    }


    protected override async Task OnParametersSetAsync()
    {
        if (Activity != null)
        {
            DateTimeOffset browserTime = await BrowserTime.ConvertToLocal(Activity.Details.StartDate);
            ActivityTime = browserTime.DateTime.ToString("G");
        }
    }
}
