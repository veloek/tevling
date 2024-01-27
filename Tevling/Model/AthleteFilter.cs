namespace Tevling.Model;

public record AthleteFilter(
    int? FollowedBy = null,
    string? SearchText = null,
    IEnumerable<int>? In = null,
    IEnumerable<int>? NotIn = null);
