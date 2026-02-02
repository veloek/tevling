using Tevling.Model.Notification;

namespace Tevling.Components;

public partial class NotificationBell : ComponentBase, IDisposable
{
    [Inject] private INotificationService NotificationService { get; set; } = null!;

    [Inject] private ILogger<NotificationBell> Logger { get; set; } = null!;

    private IDisposable? _notificationSubscription;

    private List<Notification> _notifications = [];
    [Parameter] public int AthleteId { get; set; }
    
    [Parameter] public string? Text { get; set; }

    protected override async Task OnInitializedAsync()
    {
        _notifications = [.. await NotificationService.GetUnreadNotifications(AthleteId)];
        
        _notificationSubscription ??= NotificationService.GetNotificationFeed(
                AthleteId)
            .Catch<Notification, Exception>(err =>
            {
                Logger.LogError(err, "Error in Notification feed");
                return Observable.Throw<Notification>(err).Delay(TimeSpan.FromSeconds(1));
            })
            .Retry()
            .Subscribe(async void (notification) =>
            {
                if (notification.Type == NotificationType.Read)
                {
                    _notifications.RemoveAll(n => n.Id == notification.NotificationReadId);
                }
                else
                {
                    _notifications.Add(notification);
                }

                await InvokeAsync(StateHasChanged);
            });
    }

    public void Dispose()
    {
        _notificationSubscription?.Dispose();
    }
}
