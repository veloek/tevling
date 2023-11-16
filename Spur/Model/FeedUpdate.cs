namespace Spur.Model;

public class FeedUpdate<T>
{
    public required T Item { get; init; }
    public required FeedAction Action { get; init; }
}
