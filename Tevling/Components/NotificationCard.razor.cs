using Microsoft.AspNetCore.Components;
using Tevling.Model.Notification;

namespace Tevling.Components;

public partial class NotificationCard : ComponentBase
{
    [Inject] private IBrowserTime BrowserTime { get; set; } = null!;
    
    [Parameter] public Notification? Notification { get; set; }
    
    private string? NotificationTime { get; set; }
    
    protected override async Task OnParametersSetAsync()
    {
        if (Notification != null)
        {
            DateTimeOffset browserTime = await BrowserTime.ConvertToLocal(Notification.Created);
            NotificationTime = browserTime.DateTime.ToString("G");
        }
    }
}

