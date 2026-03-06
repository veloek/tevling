using System.Reactive.Subjects;
using Microsoft.EntityFrameworkCore;

namespace Tevling.Services;

public class NotificationService(IDbContextFactory<DataContext> dataContextFactory)
    : INotificationService
{
    private readonly Subject<FeedUpdate<Notification>> _notificationFeed = new();
    private const int DaysAfterReadBeforeDelete = 5;

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

        DateTimeOffset cutoff = DateTimeOffset.Now.AddDays(-DaysAfterReadBeforeDelete);

        IQueryable<Notification> oldNotifications = dataContext.Notifications
            .Where(n =>
                n.RecipientId == athleteId &&
                n.Read.HasValue &&
                n.Read.Value < cutoff);

        await dataContext.RemoveNotificationsAsync(oldNotifications, ct);
    }

    public async Task<IReadOnlyList<Notification>> GetNotifications(int athleteId,
        CancellationToken ct = default)
    {
        await using DataContext dataContext = await dataContextFactory.CreateDbContextAsync(ct);

        return await dataContext.Notifications
            .Include(n => n.CreatedBy)
            .Include(n => n.Recipient)
            .Where(n => n.RecipientId == athleteId)
            .ToListAsync(cancellationToken: ct);
    }

    public async Task<IReadOnlyList<Notification>> GetUnreadNotifications(int athleteId,
        CancellationToken ct = default)
    {
        await using DataContext dataContext = await dataContextFactory.CreateDbContextAsync(ct);

        return await dataContext.Notifications
            .Include(n => n.CreatedBy)
            .Include(n => n.Recipient)
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
