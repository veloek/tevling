namespace Tevling.Model;

public record ChallengeFilter(string? SearchText, int? ByAthleteId, bool IncludeOutdatedChallenges, bool OnlyJoinedChallenges = false);
