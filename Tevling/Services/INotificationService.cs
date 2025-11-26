using Tevling.Model.Notification;

namespace Tevling.Services;

public interface INotificationService
{
    public Task Publish(IReadOnlyCollection<Notification> notifications, CancellationToken ct = default);
    public IObservable<Notification> GetNotificationFeed(int athleteId);

    public Task<IReadOnlyCollection<Notification>> GetNotifications(int athleteId, CancellationToken ct = default);
    public Task<IReadOnlyCollection<Notification>> GetUnreadNotifications(int athleteId, CancellationToken ct = default);

    public Task<ICollection<Notification>> MarkNotificationsAsRead(int athleteId,
        CancellationToken ct = default);

    public Task RemoveOldNotifications(int athleteId, CancellationToken ct = default);
}
