namespace Spur.Model;

public class ActivityFeed
{
    public required Activity Activity { get; init; }
    public required FeedAction Action { get; init; }
}
