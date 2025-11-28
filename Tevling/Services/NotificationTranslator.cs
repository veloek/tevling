using Microsoft.Extensions.Localization;
using Tevling.Model.Notification;

namespace Tevling.Services;

public class NotificationTranslator(IStringLocalizer<NotificationCard> localizer)
{
    public string Translate(Notification notification) => notification.Type == NotificationType.ChallengeInvite
        ? localizer[notification.Type.ToString(), notification.ChallengeTitle!]
        : localizer[notification.Type.ToString()];
}
