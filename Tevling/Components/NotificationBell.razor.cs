using Tevling.Model.Notification;

namespace Tevling.Components;

public partial class NotificationBell : ComponentBase, IDisposable
{
    [Inject] private INotificationService NotificationService { get; set; } = null!;
    [Inject] private ILogger<NotificationBell> Logger { get; set; } = null!;

    [Parameter] public int AthleteId { get; set; }
    private int Count { get; set; }
    private IDisposable? _notificationFeedSubscription;

    protected override async Task OnInitializedAsync()
    {
        Logger.LogInformation("Initializing NotificationBell");
        SubscribeToNotificationsFeed();
        Count = (await NotificationService.GetUnreadNotifications(AthleteId)).Count;
    }

    private void SubscribeToNotificationsFeed()
    {
        _notificationFeedSubscription ??= NotificationService.GetNotificationFeed(AthleteId)
            .Catch<Notification, Exception>(err =>
            {
                Logger.LogError(err, "Error in Notification feed");
                return Observable.Throw<Notification>(err).Delay(TimeSpan.FromSeconds(1));
            })
            .Retry()
            .Subscribe(async void (notification) =>
            {
                Logger.LogInformation("New notification received in nav");
                if (notification.Type == NotificationType.Cleared)
                {
                    Count = 0;
                    await InvokeAsync(StateHasChanged);
                    return;
                }
                Count += 1;
                await InvokeAsync(StateHasChanged);
            });
    }

    public void Dispose()
    {
        Logger.LogInformation("Disposing NotificationBell");
        _notificationFeedSubscription?.Dispose();
    }
}
