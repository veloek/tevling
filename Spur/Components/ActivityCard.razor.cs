using Microsoft.AspNetCore.Components;
using Spur.Model;
using Spur.Services;

namespace Spur.Components;

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
                    return $"{distanceInKm:F1} km";
                }

                TimeSpan elapsedTime = TimeSpan.FromSeconds(Activity.Details.ElapsedTimeInSeconds);
                return $"{elapsedTime:g}";
            }

            return string.Empty;
        }
    }

    protected override async Task OnParametersSetAsync()
    {
        if (Activity != null)
        {
            DateTimeOffset browserTime = await BrowserTime.ConvertToLocal(Activity.Details.StartDate);
            ActivityTime = browserTime.ToString("yyyy'-'MM'-'dd' 'HH':'mm':'ss");
        }
    }
}
