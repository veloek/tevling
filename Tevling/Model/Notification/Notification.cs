namespace Tevling.Model.Notification;

public class Notification
{
    public Guid Id { get; } = Guid.NewGuid();
    public required DateTimeOffset Created { get; init; }
    public required int CreatedBy { get; init; }
    public required IReadOnlyCollection<int> Recipients { get; init; }
    public required NotificationType Type { get; init; } 
    public NotificationState State { get; set; } = NotificationState.Unread;
    public required bool Actionable { get; init; }
    public string? Message { get; init; }
}
