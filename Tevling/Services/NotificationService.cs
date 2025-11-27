using System.Collections.Immutable;
using System.Reactive.Subjects;
using Microsoft.EntityFrameworkCore;
using Tevling.Model.Notification;

namespace Tevling.Services;

public class NotificationService(IDbContextFactory<DataContext> dataContextFactory)
    : INotificationService
{
    private readonly Subject<Notification> _notificationFeed = new();
    private const int CutoffDays = -5;

    public IObservable<Notification> GetNotificationFeed(int athleteId)
    {
        return _notificationFeed.AsObservable().Where(n => n.Recipient == athleteId);
    }
    

    public async Task Publish(IReadOnlyCollection<Notification> notifications, CancellationToken ct = default)
    {
        
        ImmutableList<Notification> notificationsForSaving =
            [.. notifications.Where(n => n.Type != NotificationType.Read)];
        await using DataContext dataContext = await dataContextFactory.CreateDbContextAsync(ct);
        await dataContext.AddNotificationsAsync(notificationsForSaving, ct);

        ImmutableList<Notification> notificationsForUpdating =
            [.. notifications.Where(n => n.Type == NotificationType.Read)];
        await dataContext.MarkNotificationsAsReadAsync(notificationsForUpdating, ct);
        
        foreach (Notification notification in notifications)
        {
            Athlete athlete = await dataContext.Athletes
                    .FirstOrDefaultAsync(a => a.Id == notification.CreatedById, ct) ??
                throw new Exception($"Unknown athlete ID {notification.CreatedById}");
            notification.CreatedBy = athlete;
            _notificationFeed.OnNext(notification);
        }
    }

    public async Task<ICollection<Notification>> MarkNotificationsAsRead(IReadOnlyCollection<Notification> notifications,
        CancellationToken ct = default)
    {
        await using DataContext dataContext = await dataContextFactory.CreateDbContextAsync(ct);
        return await dataContext.MarkNotificationsAsReadAsync(notifications, ct);
    }
    
    public async Task<ICollection<Notification>> MarkNotificationsAsRead(int athleteId,
        CancellationToken ct = default)
    {
        await using DataContext dataContext = await dataContextFactory.CreateDbContextAsync(ct);
        return await dataContext.MarkNotificationsAsReadAsync(athleteId, ct);
    }

    public async Task RemoveOldNotifications(int athleteId, CancellationToken ct = default)
    {
        await using DataContext dataContext = await dataContextFactory.CreateDbContextAsync(ct);
        DateTimeOffset cutoff = DateTimeOffset.Now.AddDays(CutoffDays);

        dataContext.UnreadNotifications.RemoveRange(
            dataContext.UnreadNotifications.Where(n =>
                n.Recipient == athleteId && n.Created < cutoff && n.Read != null));

        _ = await dataContext.SaveChangesAsync(ct);
    }
    
    public async Task<IReadOnlyCollection<Notification>> GetNotifications(int athleteId,
        CancellationToken ct = default)
    {
        await using DataContext dataContext = await dataContextFactory.CreateDbContextAsync(ct);
        DateTimeOffset cutoff = DateTimeOffset.Now.AddDays(CutoffDays);
        return await dataContext.UnreadNotifications
            .Include(n => n.CreatedBy)
            .Where(n => n.Recipient == athleteId)
            .Where(n => n.Read == null ||  n.Created > cutoff )
            .ToListAsync(cancellationToken: ct);
    }
    
    public async Task<IReadOnlyCollection<Notification>> GetUnreadNotifications(int athleteId,
        CancellationToken ct = default)
    {
        await using DataContext dataContext = await dataContextFactory.CreateDbContextAsync(ct);
        return await dataContext.UnreadNotifications
            .Where(n => n.Recipient == athleteId)
            .Where(n => n.Read == null)
            .ToListAsync(cancellationToken: ct);
    }
}
