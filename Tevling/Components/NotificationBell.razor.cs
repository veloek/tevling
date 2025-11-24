using Tevling.Model.Notification;

namespace Tevling.Components;

public partial class NotificationBell : ComponentBase, IDisposable
{
    [Inject] private INotificationService NotificationService { get; set; } = null!;
    [Inject] private ILogger<NotificationBell> Logger { get; set; } = null!;
    
    [Inject] private INotificationStateService NotificationStateService { get; set; } = null!;

    [Parameter] public int AthleteId { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await NotificationStateService.InitAsync();
        NotificationStateService.OnChange += StateHasChanged;
    }
    public void Dispose()
    {
        NotificationStateService.OnChange -= StateHasChanged;
    }
}
