using Tevling.Model.Notification;

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

        _notificationSubscription ??= NotificationService.GetNotificationFeed(
                Athlete.Id)
            .Catch<Notification, Exception>(err =>
            {
                Logger.LogError(err, "Error in Notification feed");
                return Observable.Throw<Notification>(err).Delay(TimeSpan.FromSeconds(1));
            })
            .Retry()
            .Subscribe(async void (notification) =>
            {
                if (notification.Type == NotificationType.Read) return;
                _notifications.Add(notification);
                await InvokeAsync(StateHasChanged);
            });

        List<Notification> readNotification =
            [
                .. _notifications.Select(n => new Notification
                {
                    Created = DateTimeOffset.Now,
                    CreatedById = Athlete.Id,
                    Recipient = Athlete.Id,
                    Type = NotificationType.Read,
                    NotificationReadId = n.Id,
                }),
            ];

        await NotificationService.Publish(readNotification);
    }


    public void Dispose()
    {
        _notificationSubscription?.Dispose();
    }
}
