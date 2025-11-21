using Tevling.Model.Notification;

namespace Tevling.Services;

public interface INotificationStateService
{
    int UnreadCount { get; }
    IReadOnlyCollection<Notification> Notifications { get; }

    event Action? OnChange;

    public void MarkAllAsRead();

    public Task Subscribe();
    
}
