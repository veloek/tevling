namespace Spur.Model;

public class ActivityFeedUpdate
{
    public required Activity Activity { get; init; }
    public required FeedAction Action { get; init; }
}
