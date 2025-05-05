namespace Tevling.Model;

public record ActivityFilter(int AthleteId, bool IncludeFollowing, int Months=0);
