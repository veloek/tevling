namespace Tevling.RateLimiting;

public class NotFoundRateLimitOptions
{
    public int MaxNotFoundRequests { get; init; } = 20;
    public TimeSpan Window { get; init; } = TimeSpan.FromMinutes(1);
}
