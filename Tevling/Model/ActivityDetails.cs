using Tevling.Strava;

namespace Tevling.Model;

public class ActivityDetails
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public float DistanceInMeters { get; set; }
    public int MovingTimeInSeconds { get; set; }
    public int ElapsedTimeInSeconds { get; set; }
    public float TotalElevationGain { get; set; }
    public float Calories { get; set; }
    public ActivityType Type { get; set; }
    public DateTimeOffset StartDate { get; set; }
    public bool Manual { get; set; }
}
