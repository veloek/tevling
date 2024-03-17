namespace Tevling.Components;

public partial class ActivityCard : ComponentBase
{
    [Inject] private IBrowserTime BrowserTime { get; set; } = null!;

    [Parameter] public Activity? Activity { get; set; }

    private string? ActivityTime;

    private string Stats
    {
        get
        {
            if (Activity is null) return string.Empty;
            if (Activity.Details.DistanceInMeters > 0)
            {
                float distanceInKm = Activity.Details.DistanceInMeters / 1000;
                return $"Distance: {distanceInKm:F1} km";
            }

            if (Activity.Details.TotalElevationGain > 0)
            {
                return $"Elevation: {Activity.Details.TotalElevationGain} m";
            }

            return Activity.Details.Calories > 0 ? $"Calories: {Activity.Details.Calories} kcal" : string.Empty;
        }
    }

    private string Time
    {
        get
        {
            if (Activity is null || Activity.Details.ElapsedTimeInSeconds <= 0) return string.Empty;

            TimeSpan timeSpan = TimeSpan.FromSeconds(Activity.Details.ElapsedTimeInSeconds);
            string formattedTime = "";

            if (timeSpan.Hours > 0)
                formattedTime += $"{timeSpan.Hours}h ";

            if (timeSpan.Minutes > 0 || timeSpan.Hours > 0) // Include minutes if there are any hours
                formattedTime += $"{timeSpan.Minutes}m ";

            formattedTime += $"{timeSpan.Seconds}s";

            return $"Time: {formattedTime}";
        }
    }


    protected override async Task OnParametersSetAsync()
    {
        if (Activity != null)
        {
            DateTimeOffset browserTime = await BrowserTime.ConvertToLocal(Activity.Details.StartDate);
            ActivityTime = browserTime.ToString("dd.MM.yyyy HH:mm");
        }
    }
}
