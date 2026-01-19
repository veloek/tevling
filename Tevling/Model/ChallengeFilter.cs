using Tevling.Strava;

namespace Tevling.Model;

public record ChallengeFilter(
    string? SearchText,
    int? ByAthleteId,
    bool IncludeOutdatedChallenges,
    bool OnlyJoinedChallenges = false,
    bool IncludeTimeChallenges = true,
    bool IncludeElevationChallenges = true,
    bool IncludeDistanceChallenges = true,
    bool IncludeCalorieChallenges = true,
    IReadOnlyCollection<ActivityType>? ActivityTypes = null
    );
