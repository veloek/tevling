namespace Tevling.Model.Notification;

public class Notification
{
    public Guid Id { get; } = Guid.NewGuid();
    public required DateTimeOffset Created { get; init; }
    public required int CreatedById { get; init; }
    public Athlete? CreatedBy { get; set; }
    public required int Recipient { get; init; }

    public Guid? NotificationReadId { get; init; }
    
    public string? ChallengeTitle { get; set; }

    public DateTimeOffset? Read { get; set; }
    public required NotificationType Type { get; init; }

    public string Message()
    {
        return Type switch
        {
            NotificationType.FollowRequestCreated => "Wants to follow you!",
            NotificationType.FollowRequestAccepted => "Accepted your follow request!",
            NotificationType.ChallengeInvite => $"Invited you to join the \"{ChallengeTitle}\" challenge ",
            _ => "",
        };
    }
}
