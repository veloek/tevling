using Tevling.Model.Notification;

namespace Tevling.Services;

public interface INotificationService
{
    public void Publish(Notification notification);
    public IObservable<Notification> GetNotificationFeed(int athleteId);

    public IReadOnlyCollection<Notification> GetUnreadNotifications(int athleteId);
}
