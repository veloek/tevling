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
            .Catch<FeedUpdate<Notification>, Exception>(ex =>
            {
                Logger.LogError(ex, "Error in notification feed");
                return Observable.Throw<FeedUpdate<Notification>>(ex).Delay(TimeSpan.FromSeconds(1));
            })
            .Retry()
            .Subscribe(update =>
            {
                if (update.Action == FeedAction.Create)
                {
                    _notifications.Add(update.Item);
                }
                else if (update.Action == FeedAction.Update && update.Item.Read is not null)
                {
                    _notifications.RemoveAll(n => n.Id == update.Item.Id);
                }

                InvokeAsync(StateHasChanged);
            });
    }

    public void Dispose()
    {
        _notificationSubscription?.Dispose();
    }
}
