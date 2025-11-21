using Tevling.Model.Notification;

namespace Tevling.Pages;

public partial class Notifications : ComponentBase, IDisposable
{
    [Inject] private INotificationService NotificationService { get; set; } = null!;
    [Inject] private IAuthenticationService AuthenticationService { get; set; } = null!;
    [Inject] private ILogger<Notifications> Logger { get; set; } = null!;

    [Inject] private INotificationStateService NotificationStateService { get; set; } = null!;

    private ICollection<Notification> UnreadNotifications { get; set; } = [];
    private IDisposable? _notificationsFeedSubscription;
    private Athlete _athlete = default!;

    protected override async Task OnInitializedAsync()
    {
        _athlete = await AuthenticationService.GetCurrentAthleteAsync();

        // UnreadNotifications = [.. await NotificationService.GetUnreadNotifications(_athlete.Id)];
        // await NotificationService.MarkNotificationsAsRead([..UnreadNotifications]);
        // SubscribeToNotificationFeed();
        await NotificationStateService.Subscribe();
        NotificationStateService.MarkAllAsRead();
        NotificationStateService.OnChange += StateHasChanged;
    }

    private void SubscribeToNotificationFeed()
    {
        _notificationsFeedSubscription ??= NotificationService.GetNotificationFeed(_athlete.Id)
            .Catch<Notification, Exception>(err =>
            {
                Logger.LogError(err, "Error in Notifications feed");
                return Observable.Throw<Notification>(err).Delay(TimeSpan.FromSeconds(1));
            })
            .Retry()
            .Subscribe(async void (notification) =>
            {
                if (notification.Type == NotificationType.Cleared) return;
                Logger.LogInformation("New notification received in notifications page");
                UnreadNotifications.Add(notification);
                await NotificationService.MarkNotificationsAsRead([notification]);

                await InvokeAsync(StateHasChanged);
            });
    }

    public void Dispose()
    {
        _notificationsFeedSubscription?.Dispose();
        NotificationStateService.OnChange -= StateHasChanged;
    }
}
