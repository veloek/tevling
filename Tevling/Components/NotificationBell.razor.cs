namespace Tevling.Components;

public partial class NotificationBell : ComponentBase
{
    [Inject] private INotificationService NotificationService { get; set; } = null!;
    [Inject] private IAthleteService AthleteService { get; set; } = null!;
    [Inject] private ILogger<NotificationBell> Logger { get; set; } = null!;
    
    [Parameter] public int AthleteId { get; set; }
    private int Count { get; set; }
    private IDisposable? _athleteFollowersFeedSubscription;
    
    protected override async Task OnParametersSetAsync()
    {
        Count = await NotificationService.GetNotificationCount(AthleteId);
        SubscribeToAthleteFollowersFeed();
    }
    
    private void SubscribeToAthleteFollowersFeed()
    {
        _athleteFollowersFeedSubscription = AthleteService.GetAthleteFollowersFeed(AthleteId)
            .Catch<FeedUpdate<FollowRequest>, Exception>(
                err =>
                {
                    Logger.LogError(err, "Error in followers feed");
                    return Observable.Throw<FeedUpdate<FollowRequest>>(err).Delay(TimeSpan.FromSeconds(1));
                })
            .Retry()
            .Subscribe(async void (feed) =>
                {
                    Logger.LogInformation("New notification received");
                    Count = await NotificationService.GetNotificationCount(AthleteId);
                    await InvokeAsync(StateHasChanged);
                });
    }
    
    public void Dispose()
    {
        _athleteFollowersFeedSubscription?.Dispose();
    }
}

