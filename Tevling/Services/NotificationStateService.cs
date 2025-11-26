using Tevling.Model.Notification;

namespace Tevling.Services;

public class NotificationStateService(
    IAuthenticationService authenticationService,
    INotificationService notificationService,
    ILogger<NotificationStateService> logger) : INotificationStateService, IDisposable
{
    private readonly List<Notification> _notifications = [];
    public event Action? OnChange;

    public IReadOnlyList<Notification> Notifications => _notifications;
    public int UnreadCount => _notifications.Count(n => n.Read == null);
    private IDisposable? _notificationSubscription;

    private int? _athleteId;

    public async Task InitAsync()
    {
        _athleteId ??= (await authenticationService.GetCurrentAthleteAsync()).Id;

        _notifications.Clear();
        _notifications.AddRange(await notificationService.GetNotifications(_athleteId!.Value));

        // Subscribe to the NotificationService's notification stream
        _notificationSubscription ??= notificationService.GetNotificationFeed(
                _athleteId!.Value)
            .Catch<Notification, Exception>(err =>
            {
                logger.LogError(err, "Error in Notification feed");
                return Observable.Throw<Notification>(err).Delay(TimeSpan.FromSeconds(1));
            })
            .Retry()
            .Subscribe(async void (notification) => { AddNotification(notification); });
    }

    public async Task RefreshAsync()
    {
        _notifications.Clear();
        _notifications.AddRange(await notificationService.GetNotifications(_athleteId!.Value));
        OnChange?.Invoke();
    }
    

    public async Task MarkAllAsRead()
    {
        await notificationService.RemoveOldNotifications(_athleteId!.Value);
        _notifications.Clear();
        _notifications.AddRange(await notificationService.GetNotifications(_athleteId!.Value));
        _ = await notificationService.MarkNotificationsAsRead(_athleteId!.Value);

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
