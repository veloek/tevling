namespace Tevling.Model;

public record ActivityFilter(int AthleteId, bool IncludeFollowing, DateTimeOffset? From = null);
