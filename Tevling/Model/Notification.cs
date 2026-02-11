namespace Tevling.Model;

public abstract class Notification
{
    public int Id { get; set; }
    public DateTimeOffset Created { get; set; }
    public DateTimeOffset? Read { get; set; }

    public int? CreatedById { get; set; }
    public Athlete? CreatedBy { get; set; }
    public int RecipientId { get; set; }
    public Athlete? Recipient { get; set; }
}

public class NewFollowRequest : Notification;

public class AcceptedFollowRequest : Notification;

public class ChallengeInvite : Notification
{
    public int ChallengeId { get; set; }
    public Challenge? Challenge { get; set; }
}
