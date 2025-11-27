namespace Tevling.Model.Notification.Message;

public class FollowRequestMessage : INotificationMessage
{
    public string Message() => "Wants to follow you!";
}
