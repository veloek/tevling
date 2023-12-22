namespace Spur.Model;

public record ChallengeFilter(string? SearchText, int? ByAthleteId, bool IncludeOutdatedChallenges);
