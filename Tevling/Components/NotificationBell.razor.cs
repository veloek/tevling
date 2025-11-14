using Tevling.Model.Notification;

namespace Tevling.Components;

public partial class NotificationBell : ComponentBase
{
    [Inject] private INotificationService NotificationService { get; set; } = null!;
    [Inject] private ILogger<NotificationBell> Logger { get; set; } = null!;
    
    [Parameter] public int AthleteId { get; set; }
    private ICollection<Notification> UnreadNotifications { get; set; } = [];
    private int Count { get; set; }
    private IDisposable? _notificationFeedSubscription;
    
    protected override async Task OnParametersSetAsync()
    {
        SubscribeToAthleteFollowersFeed();
        UnreadNotifications = [.. NotificationService.GetUnreadNotifications(AthleteId)];
        Count = UnreadNotifications.Count;
    }
    
    private void SubscribeToAthleteFollowersFeed()
    {
        _notificationFeedSubscription = NotificationService.GetNotificationFeed(AthleteId)
            .Catch<Notification, Exception>(
                err =>
                {
                    Logger.LogError(err, "Error in Notification feed");
                    return Observable.Throw<Notification>(err).Delay(TimeSpan.FromSeconds(1));
                })
            .Retry()
            .Subscribe(async void (notification) =>
            {
                switch (notification.State)
                    {
                        case NotificationState.Unread:
                            Logger.LogInformation("New notification received");
                            UnreadNotifications.Add(notification);
                            break;
                        case NotificationState.ActedUpon:
                        case NotificationState.Read:
                            Logger.LogInformation("New notification received");
                            
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                Count = UnreadNotifications.Count;

                await InvokeAsync(StateHasChanged);
            });
    }
    
    public void Dispose()
    {
        _notificationFeedSubscription?.Dispose();
    }
}

