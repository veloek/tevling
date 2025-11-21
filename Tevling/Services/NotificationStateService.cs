using Tevling.Model.Notification;

namespace Tevling.Services;

public class NotificationStateService (IAuthenticationService authenticationService,
    INotificationService notificationService,
    ILogger<NotificationStateService> logger) : INotificationStateService, IDisposable
{

    
    private readonly List<Notification> _notifications = [];
    public event Action? OnChange;
    
    public IReadOnlyCollection<Notification> Notifications => _notifications;
    public int UnreadCount => _notifications.Count;
    private IDisposable? _notificationSubscription;
    
    
    public async Task Subscribe()
    {
        // Subscribe to the NotificationService's notification stream
        _notificationSubscription ??= notificationService.GetNotificationFeed(
                (await authenticationService.GetCurrentAthleteAsync()).Id)
            .Catch<Notification, Exception>(err =>
            {
                logger.LogError(err, "Error in Notification feed");
                return Observable.Throw<Notification>(err).Delay(TimeSpan.FromSeconds(1));
            })
            .Retry()
            .Subscribe(async void (notification) => { AddNotification(notification); });
    }

    
    public void MarkAllAsRead()
    {
        _notifications.Clear();
        OnChange?.Invoke();
    }
    
    private void AddNotification(Notification notification)
    {
        _notifications.Add(notification);
        OnChange?.Invoke();
    }
    
    public void Dispose()
    {
        _notificationSubscription?.Dispose();   
    }
    
}
