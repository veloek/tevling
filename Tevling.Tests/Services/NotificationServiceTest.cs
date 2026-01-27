using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Tevling.Data;
using Tevling.Model;
using Tevling.Model.Notification;
using Xunit;

namespace Tevling.Services;

public class NotificationServiceTest
{
    [Fact]
    public async Task GetNotifications_Should_Return_Unread_or_new_Notifications()
    {
        IDbContextFactory<DataContext> dataContextFactory = new InMemoryDataContextFactory();
        NotificationService sut = CreateSut(dataContextFactory);

        const int createdById = 1;
        const int recipientId = 2;

        await using (DataContext context =
                     await dataContextFactory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            context.Athletes.Add(new Athlete { Id = createdById });
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        Notification newUnreadNotification = new()
        {
            Created = DateTimeOffset.Now,
            CreatedById = createdById,
            Recipient = recipientId,
            Type = NotificationType.FollowRequestCreated,
        };

        Notification newReadNotification = new()
        {
            Created = DateTimeOffset.Now,
            CreatedById = createdById,
            Recipient = recipientId,
            Read = DateTimeOffset.Now,
            Type = NotificationType.FollowRequestCreated,
        };
        Notification oldUnreadNotification = new()
        {
            Created = DateTimeOffset.Now.AddDays(-6),
            CreatedById = createdById,
            Recipient = recipientId,
            Type = NotificationType.FollowRequestCreated,
        };
        Notification oldReadNotification = new()
        {
            Created = DateTimeOffset.Now.AddDays(-6),
            CreatedById = createdById,
            Recipient = recipientId,
            Read = DateTimeOffset.Now,
            Type = NotificationType.FollowRequestCreated,
        };

        await sut.Publish(
            [newUnreadNotification, oldUnreadNotification, oldReadNotification, newReadNotification],
            TestContext.Current.CancellationToken);
        IReadOnlyCollection<Notification> notifications = await sut.GetNotifications(
            recipientId,
            TestContext.Current.CancellationToken);
        notifications.Count.ShouldBe(3);
        notifications.Select(n => n.Id).ShouldContain(newUnreadNotification.Id);
        notifications.Select(n => n.Id).ShouldContain(newReadNotification.Id);
        notifications.Select(n => n.Id).ShouldContain(oldUnreadNotification.Id);
    }
    
    [Fact]
    public async Task GetUnreadNotifications_Should_Return_Unread_Notifications()
    {
        IDbContextFactory<DataContext> dataContextFactory = new InMemoryDataContextFactory();
        NotificationService sut = CreateSut(dataContextFactory);

        const int createdById = 1;
        const int recipientId = 2;

        await using (DataContext context =
                     await dataContextFactory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            context.Athletes.Add(new Athlete { Id = createdById });
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        Notification newUnreadNotification = new()
        {
            Created = DateTimeOffset.Now,
            CreatedById = createdById,
            Recipient = recipientId,
            Type = NotificationType.FollowRequestCreated,
        };

        Notification newReadNotification = new()
        {
            Created = DateTimeOffset.Now,
            CreatedById = createdById,
            Recipient = recipientId,
            Read = DateTimeOffset.Now,
            Type = NotificationType.FollowRequestCreated,
        };
        Notification oldUnreadNotification = new()
        {
            Created = DateTimeOffset.Now.AddDays(-6),
            CreatedById = createdById,
            Recipient = recipientId,
            Type = NotificationType.FollowRequestCreated,
        };
        Notification oldReadNotification = new()
        {
            Created = DateTimeOffset.Now.AddDays(-6),
            CreatedById = createdById,
            Recipient = recipientId,
            Read = DateTimeOffset.Now,
            Type = NotificationType.FollowRequestCreated,
        };

        await sut.Publish(
            [newUnreadNotification, oldUnreadNotification, oldReadNotification, newReadNotification],
            TestContext.Current.CancellationToken);
        IReadOnlyCollection<Notification> notifications = await sut.GetUnreadNotifications(
            recipientId,
            TestContext.Current.CancellationToken);
        notifications.Count.ShouldBe(2);
        notifications.Select(n => n.Id).ShouldContain(newUnreadNotification.Id);
        notifications.Select(n => n.Id).ShouldContain(oldUnreadNotification.Id);
    }
    
    [Fact]
    public async Task RemoveOldNotifications_Should_Remove_read_Notifications()
    {
        IDbContextFactory<DataContext> dataContextFactory = new InMemoryDataContextFactory();
        NotificationService sut = CreateSut(dataContextFactory);

        const int createdById = 1;
        const int recipientId = 2;

        await using (DataContext context =
                     await dataContextFactory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            context.Athletes.Add(new Athlete { Id = createdById });
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        Notification newUnreadNotification = new()
        {
            Created = DateTimeOffset.Now,
            CreatedById = createdById,
            Recipient = recipientId,
            Type = NotificationType.FollowRequestCreated,
        };

        Notification newReadNotification = new()
        {
            Created = DateTimeOffset.Now,
            CreatedById = createdById,
            Recipient = recipientId,
            Read = DateTimeOffset.Now,
            Type = NotificationType.FollowRequestCreated,
        };
        Notification oldUnreadNotification = new()
        {
            Created = DateTimeOffset.Now.AddDays(-6),
            CreatedById = createdById,
            Recipient = recipientId,
            Type = NotificationType.FollowRequestCreated,
        };
        Notification oldReadNotification = new()
        {
            Created = DateTimeOffset.Now.AddDays(-6),
            CreatedById = createdById,
            Recipient = recipientId,
            Read = DateTimeOffset.Now,
            Type = NotificationType.FollowRequestCreated,
        };

        await sut.Publish(
            [newUnreadNotification, oldUnreadNotification, oldReadNotification, newReadNotification],
            TestContext.Current.CancellationToken);
        await sut.RemoveOldNotifications(recipientId, TestContext.Current.CancellationToken);
        IReadOnlyCollection<Notification> notifications = await sut.GetNotifications(
            recipientId,
            TestContext.Current.CancellationToken);
        notifications.Count.ShouldBe(3);
        notifications.Select(n => n.Id).ShouldContain(newUnreadNotification.Id);
        notifications.Select(n => n.Id).ShouldContain(newReadNotification.Id);
        notifications.Select(n => n.Id).ShouldContain(oldUnreadNotification.Id);
    }

    private static NotificationService CreateSut(IDbContextFactory<DataContext> dataContextFactory) =>
        new(dataContextFactory);
}
