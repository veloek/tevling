using Microsoft.Extensions.Localization;

namespace Tevling.Services;

public class NotificationTranslator(IStringLocalizer<NotificationCard> localizer)
{
    public string Translate(Notification notification)
    {
        return notification switch
        {
            ChallengeInvite challengeInvite => localizer[nameof(ChallengeInvite), challengeInvite.Challenge?.Title ?? ""],
            _ => localizer[notification.GetType().Name]
        };
    }
}
