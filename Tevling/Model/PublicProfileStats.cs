using Tevling.Strava;

namespace Tevling.Model;

public record PublicProfileStats
{
    public double? LongestRun { get; init; }
    public double? LongestWalk { get; init; }
    public double? LongestRide { get; init; }
    public double? BiggestClimb { get; init; }
    public double? LongestActivity { get; init; }
    public int? NumberOfActivitiesLogged { get; init; }
    public ActivityType? MostPopularActivity { get; init; }
}
