using Tevling.Strava;

namespace Tevling.Utils;

public static class ActivityTypeExt
{
    public static string ToString(this ActivityType activityType)
    {
        return activityType switch
        {
            ActivityType.AlpineSki => "Alpine ski",
            ActivityType.BackcountrySki => "Backcountry ski",
            ActivityType.EBikeRide => "E-bike ride",
            ActivityType.IceSkate => "Ice skate",
            ActivityType.InlineSkate => "Inline skate",
            ActivityType.NordicSki => "Nordic ski",
            ActivityType.RockClimbing => "Rock climbing",
            ActivityType.RollerSki => "Roller ski",
            ActivityType.StairStepper => "Stair stepper",
            ActivityType.StandUpPaddling => "SUP",
            ActivityType.VirtualRide => "Virtual ride",
            ActivityType.VirtualRun => "Virtual run",
            ActivityType.WaterSport => "Water sport",
            ActivityType.WeightTraining => "Weight training",
            _ => activityType.ToString(),
        };
    }
}
