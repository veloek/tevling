namespace Tevling.Pages;

public partial class Notifications : ComponentBase, IAsyncDisposable
{
    [Inject] private IAuthenticationService AuthenticationService { get; set; } = null!;

    [Inject] private INotificationService NotificationService { get; set; } = null!;

    [Inject] private ILogger<Notifications> Logger { get; set; } = null!;

    private IDisposable? _notificationSubscription;

    private List<Notification> _notifications = [];
    private Athlete? Athlete { get; set; }
    private Notification[] NotificationList { get; set; } = [];


    protected override async Task OnInitializedAsync()
    {
        Athlete = await AuthenticationService.GetCurrentAthleteAsync();

        await NotificationService.RemoveOldNotifications(Athlete.Id);
        IReadOnlyList<Notification> notifications = await NotificationService.GetNotifications(Athlete.Id);
        AddNotifications([..notifications]);

        _notificationSubscription ??= NotificationService.GetNotificationFeed(Athlete.Id)
            .Catch<FeedUpdate<Notification>, Exception>(ex =>
            {
                Logger.LogError(ex, "Error in Notification feed");
                return Observable.Throw<FeedUpdate<Notification>>(ex).Delay(TimeSpan.FromSeconds(1));
            })
            .Retry()
            .Subscribe(feed =>
            {
                switch (feed.Action)
                {
                    case FeedAction.Create:
                        AddNotifications(feed.Item);
                        break;
                    case FeedAction.Update:
                        ReplaceNotification(feed.Item);
                        break;
                    case FeedAction.Delete:
                        RemoveNotification(feed.Item);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException("Unknown activity feed action: " + feed.Action);
                }
            });
    }

    private void AddNotifications(params Notification[] notifications)
    {
        _notifications.AddRange(notifications);

        UpdateNotifications();
    }

    private void ReplaceNotification(Notification notification)
    {
        _notifications.RemoveAll(a => a.Id == notification.Id);
        _notifications.Add(notification);

        UpdateNotifications();
    }

    private void RemoveNotification(Notification notification)
    {
        _notifications.RemoveAll(a => a.Id == notification.Id);

        UpdateNotifications();
    }

    private void UpdateNotifications()
    {
        // Even if notifications from the DB are filtered and sorted, we need to
        // filter and sort here as well due to added notifications from the feed.
        NotificationList = [.. _notifications
            .OrderBy(n => n.Read is not null)
            .ThenByDescending(n => n.Created)];

        InvokeAsync(StateHasChanged);
    }

    private async Task MarkNotificationsAsRead()
    {
        foreach (Notification unreadNotification in _notifications.Where(n => n.Read is null))
        {
            await NotificationService.MarkNotificationAsRead(unreadNotification, CancellationToken.None);
        }
    }

    public async ValueTask DisposeAsync()
    {
        _notificationSubscription?.Dispose();

        await MarkNotificationsAsRead();
    }
}
