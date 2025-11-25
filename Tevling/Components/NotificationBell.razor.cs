namespace Tevling.Components;

public partial class NotificationBell : ComponentBase, IDisposable
{
    [Inject] private INotificationStateService NotificationStateService { get; set; } = null!;

    [Parameter] public int AthleteId { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await NotificationStateService.InitAsync();
        NotificationStateService.OnChange += HandleNotificationChanged;
    }

    private void HandleNotificationChanged()
    {
        _ = InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        NotificationStateService.OnChange -= HandleNotificationChanged;
    }
}
