using System.Reactive.Subjects;
using Microsoft.EntityFrameworkCore;

namespace Tevling.Services;

public class NotificationService(IDbContextFactory<DataContext> dataContextFactory)
    : INotificationService
{
    private readonly Subject<FeedUpdate<Notification>> _notificationFeed = new();
    private const int CutoffDays = 5;

    public IObservable<FeedUpdate<Notification>> GetNotificationFeed(int athleteId)
    {
        return _notificationFeed.AsObservable()
            .Where(update => update.Item.RecipientId == athleteId);
    }

    public async Task<TNotification> Publish<TNotification>(TNotification notification, CancellationToken ct = default)
        where TNotification : Notification
    {
        await using DataContext dataContext = await dataContextFactory.CreateDbContextAsync(ct);
        notification = await dataContext.AddNotificationAsync(notification, ct);
        _notificationFeed.OnNext(new FeedUpdate<Notification> { Item = notification, Action = FeedAction.Create });
        return notification;
    }

    public async Task RemoveOldNotifications(int athleteId, CancellationToken ct = default)
    {
        await using DataContext dataContext = await dataContextFactory.CreateDbContextAsync(ct);
        DateTimeOffset cutoff = DateTimeOffset.Now.AddDays(-CutoffDays);

        dataContext.Notifications.RemoveRange(
            dataContext.Notifications
                .Where(n => n.RecipientId == athleteId && n.Created < cutoff && n.Read != null));

        _ = await dataContext.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyCollection<Notification>> GetNotifications(int athleteId,
        CancellationToken ct = default)
    {
        await using DataContext dataContext = await dataContextFactory.CreateDbContextAsync(ct);
        DateTimeOffset cutoff = DateTimeOffset.Now.AddDays(-CutoffDays);

        return await dataContext.Notifications
            .Include(n => n.CreatedBy)
            .Where(n => n.RecipientId == athleteId)
            .Where(n => n.Read == null || n.Created > cutoff)
            .ToListAsync(cancellationToken: ct);
    }

    public async Task<IReadOnlyCollection<Notification>> GetUnreadNotifications(int athleteId,
        CancellationToken ct = default)
    {
        await using DataContext dataContext = await dataContextFactory.CreateDbContextAsync(ct);

        return await dataContext.Notifications
            .Where(n => n.RecipientId == athleteId)
            .Where(n => n.Read == null)
            .ToListAsync(cancellationToken: ct);
    }

    public async Task<TNotification> MarkNotificationAsRead<TNotification>(TNotification notification, CancellationToken ct = default)
        where TNotification : Notification
    {
        await using DataContext dataContext = await dataContextFactory.CreateDbContextAsync(ct);
        notification.Read = DateTimeOffset.UtcNow;
        notification = await dataContext.UpdateNotificationAsync(notification, ct);
        _notificationFeed.OnNext(new FeedUpdate<Notification> { Item = notification, Action = FeedAction.Update });
        return notification;
    }
}
