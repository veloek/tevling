using Tevling.Model;

namespace Tevling.Pages;

public partial class Notifications : ComponentBase, IDisposable
{
    [Inject] private IAuthenticationService AuthenticationService { get; set; } = null!;

    [Inject] private INotificationService NotificationService { get; set; } = null!;

    [Inject] private ILogger<Notifications> Logger { get; set; } = null!;

    private IDisposable? _notificationSubscription;

    private List<Notification> _notifications = [];
    private Athlete? Athlete { get; set; }

    protected override async Task OnInitializedAsync()
    {
        Athlete = await AuthenticationService.GetCurrentAthleteAsync();

        await NotificationService.RemoveOldNotifications(Athlete.Id);
        _notifications = [.. await NotificationService.GetNotifications(Athlete.Id)];

        _notificationSubscription ??= NotificationService.GetNotificationFeed(Athlete.Id)
            .Catch<FeedUpdate<Notification>, Exception>(ex =>
            {
                Logger.LogError(ex, "Error in Notification feed");
                return Observable.Throw<FeedUpdate<Notification>>(ex).Delay(TimeSpan.FromSeconds(1));
            })
            .Retry()
            .Subscribe(notification =>
            {
                // TODO: If new, add
                // TODO: If update, replace
                //_notifications.Add(notification);
                InvokeAsync(StateHasChanged);
            });

        // TODO: Add timer to mark notifications as read after a short time

        // await NotificationService.MarkAllNotificationsAsRead();
    }


    public void Dispose()
    {
        _notificationSubscription?.Dispose();
    }
}
