namespace Tevling.Services;

public interface INotificationService
{
    Task<TNotification> Publish<TNotification>(TNotification notification, CancellationToken ct = default)
        where TNotification : Notification;

    IObservable<FeedUpdate<Notification>> GetNotificationFeed(int athleteId);

    Task<IReadOnlyCollection<Notification>> GetNotifications(int athleteId, CancellationToken ct = default);

    Task<IReadOnlyCollection<Notification>> GetUnreadNotifications(int athleteId, CancellationToken ct = default);

    Task<TNotification> MarkNotificationAsRead<TNotification>(TNotification notification, CancellationToken ct = default)
        where TNotification : Notification;

    Task RemoveOldNotifications(int athleteId, CancellationToken ct = default);
}
