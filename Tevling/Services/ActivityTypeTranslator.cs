using Microsoft.Extensions.Localization;
using Tevling.Strava;

namespace Tevling.Services;

public class ActivityTypeTranslator(IStringLocalizer<ActivityType> localizer)
{
    public string Translate(ActivityType activityType) => localizer[activityType.ToString()];
}
