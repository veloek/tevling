using Tevling.Model.Notification;

namespace Tevling.Pages;

public partial class Notifications : ComponentBase, IDisposable
{
    [Inject] private INotificationService NotificationService { get; set; } = null!;
    [Inject] private IAuthenticationService AuthenticationService { get; set; } = null!;
    [Inject] private ILogger<Notifications> Logger { get; set; } = null!;

    [Inject] private INotificationStateService NotificationStateService { get; set; } = null!;

    private Athlete _athlete = null!;

    protected override async Task OnInitializedAsync()
    {
        _athlete = await AuthenticationService.GetCurrentAthleteAsync();
        
        await NotificationStateService.InitAsync();
        NotificationStateService.OnChange += StateHasChanged;
        await NotificationStateService.MarkAllAsRead();
    }



    public void Dispose()
    {
        NotificationStateService.OnChange -= StateHasChanged;
    }
}
