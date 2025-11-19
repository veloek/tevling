using System.Collections.Immutable;
using System.Reactive.Subjects;
using Microsoft.EntityFrameworkCore;
using Tevling.Model.Notification;

namespace Tevling.Services;

public class NotificationService(IDbContextFactory<DataContext> dataContextFactory)
    : INotificationService
{
    private readonly Subject<Notification> _notificationFeed = new();

    public IObservable<Notification> GetNotificationFeed(int athleteId)
    {
        return _notificationFeed.AsObservable().Where(n => n.Recipient == athleteId);
    }

    public async Task Publish(IReadOnlyCollection<Notification> notifications, CancellationToken ct = default)
    {
        ImmutableList<Notification> notificationsForSaving =
            [.. notifications.Where(n => n.Type != NotificationType.Cleared)];
        await using DataContext dataContext = await dataContextFactory.CreateDbContextAsync(ct);
        await dataContext.AddNotificationsAsync(notificationsForSaving, ct);

        foreach (Notification notification in notifications)
        {
            _notificationFeed.OnNext(notification);
        }
    }

    public async Task MarkNotificationsAsRead(IReadOnlyCollection<Notification> notifications,
        CancellationToken ct = default)
    {
        await using DataContext dataContext = await dataContextFactory.CreateDbContextAsync(ct);
        await dataContext.RemoveNotifications(notifications, ct);
        if (notifications.Count > 0)
        {
            await Publish(
                [
                    new Notification
                    {
                        Created = DateTimeOffset.Now,
                        CreatedBy = notifications.Select(n => n.Recipient).First(),
                        Recipient = notifications.Select(n => n.Recipient).First(),
                        Type = NotificationType.Cleared,
                    },
                ],
                ct);
        }
    }

    public async Task<IReadOnlyCollection<Notification>> GetUnreadNotifications(int athleteId,
        CancellationToken ct = default)
    {
        await using DataContext dataContext = await dataContextFactory.CreateDbContextAsync(ct);
        return await dataContext.UnreadNotifications
            .Where(n => n.Recipient == athleteId)
            .ToListAsync(cancellationToken: ct);
    }
}
