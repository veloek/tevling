
namespace Tevling.Pages;

public partial class Notifications : ComponentBase, IDisposable
{
    [Inject] private IAuthenticationService AuthenticationService { get; set; } = null!;

    [Inject] private INotificationStateService NotificationStateService { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        await NotificationStateService.InitAsync();
        NotificationStateService.OnChange += HandleNotificationsChanged;
        await NotificationStateService.MarkAllAsRead();
    }

    private void HandleNotificationsChanged()
    {
        _ = InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        NotificationStateService.OnChange -= HandleNotificationsChanged;
    }
}
