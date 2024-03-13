namespace Tevling.Components;

public partial class ActivityCard : ComponentBase
{
    [Inject]
    IBrowserTime BrowserTime { get; set; } = null!;

    [Parameter]
    public Activity? Activity { get; set; }

    private string? ActivityTime;

    private string Stats
    {
        get
        {
            if (Activity is not null)
            {
                if (Activity.Details.DistanceInMeters > 0)
                {
                    float distanceInKm = Activity.Details.DistanceInMeters / 1000;
                    return $"Distance: {distanceInKm:F1} km";
                }
            }

            return string.Empty;
        }
    }

    private string Time {
        get {
            if (Activity is not null)
            {
                TimeSpan timeSpan = TimeSpan.FromSeconds(Activity.Details.ElapsedTimeInSeconds);
                string formattedTime = "";
                
                if (timeSpan.Hours > 0)
                    formattedTime += string.Format("{0}h ", timeSpan.Hours);
                    
                if (timeSpan.Minutes > 0 || timeSpan.Hours > 0) // Include minutes if there are any hours
                    formattedTime += string.Format("{0}m ", timeSpan.Minutes);
                    
                formattedTime += string.Format("{0}s", timeSpan.Seconds);
                
                return $"Time: {formattedTime}";
            }

            return string.Empty;
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
