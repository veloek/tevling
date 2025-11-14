using System.Reactive.Subjects;
using Tevling.Model.Notification;

namespace Tevling.Services;

public class NotificationService()
    : INotificationService
{
    
    private readonly Subject<Notification> _notificationFeed = new();
    
    
    public IObservable<Notification> GetNotificationFeed(int athleteId)
    {
        return _notificationFeed.AsObservable().Where(n => n.Recipients.Contains(athleteId));
    }

    public void Publish(Notification notification)
    {
        _notificationFeed.OnNext(notification);
    }

    public IReadOnlyCollection<Notification> GetUnreadNotifications(int athleteId)
    {
        // TODO
        return [];
    }

}
