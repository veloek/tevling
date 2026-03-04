using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Tevling.Data;
using Tevling.Model;
using Xunit;

namespace Tevling.Services;

public class NotificationServiceTest
{
    [Fact]
    public async Task GetNotifications_Should_Return_All_Notifications()
    {
        IDbContextFactory<DataContext> dataContextFactory = new InMemoryDataContextFactory();
        NotificationService sut = CreateSut(dataContextFactory);

        const int createdById = 1;
        const int recipientId = 2;

        await using (DataContext context =
                     await dataContextFactory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            await context.AddAthleteAsync(new Athlete { Id = createdById }, TestContext.Current.CancellationToken);
            await context.AddAthleteAsync(new Athlete { Id = recipientId }, TestContext.Current.CancellationToken);
        }

        NewFollowRequest newUnreadNotification = new()
        {
            Created = DateTimeOffset.Now,
            CreatedById = createdById,
            RecipientId = recipientId,
        };
        NewFollowRequest newReadNotification = new()
        {
            Created = DateTimeOffset.Now,
            CreatedById = createdById,
            RecipientId = recipientId,
            Read = DateTimeOffset.Now,
        };
        NewFollowRequest oldUnreadNotification = new()
        {
            Created = DateTimeOffset.Now.AddDays(-6),
            CreatedById = createdById,
            RecipientId = recipientId,
        };
        NewFollowRequest oldReadNotification = new()
        {
            Created = DateTimeOffset.Now.AddDays(-6),
            CreatedById = createdById,
            RecipientId = recipientId,
            Read = DateTimeOffset.Now.AddDays(-6),
        };

        await sut.Publish(newUnreadNotification, TestContext.Current.CancellationToken);
        await sut.Publish(oldUnreadNotification, TestContext.Current.CancellationToken);
        await sut.Publish(oldReadNotification, TestContext.Current.CancellationToken);
        await sut.Publish(newReadNotification, TestContext.Current.CancellationToken);

        IReadOnlyList<Notification> notifications = await sut.GetNotifications(
            recipientId,
            TestContext.Current.CancellationToken);
        notifications.Count.ShouldBe(4);
        notifications.Select(n => n.Id).ShouldContain(newUnreadNotification.Id);
        notifications.Select(n => n.Id).ShouldContain(newReadNotification.Id);
        notifications.Select(n => n.Id).ShouldContain(oldUnreadNotification.Id);
        notifications.Select(n => n.Id).ShouldContain(oldReadNotification.Id);
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
            await context.AddAthleteAsync(new Athlete { Id = createdById }, TestContext.Current.CancellationToken);
            await context.AddAthleteAsync(new Athlete { Id = recipientId }, TestContext.Current.CancellationToken);
        }

        NewFollowRequest newUnreadNotification = new()
        {
            Created = DateTimeOffset.Now,
            CreatedById = createdById,
            RecipientId = recipientId,
        };

        NewFollowRequest newReadNotification = new()
        {
            Created = DateTimeOffset.Now,
            CreatedById = createdById,
            RecipientId = recipientId,
            Read = DateTimeOffset.Now,
        };
        NewFollowRequest oldUnreadNotification = new()
        {
            Created = DateTimeOffset.Now.AddDays(-6),
            CreatedById = createdById,
            RecipientId = recipientId,
        };
        NewFollowRequest oldReadNotification = new()
        {
            Created = DateTimeOffset.Now.AddDays(-6),
            CreatedById = createdById,
            RecipientId = recipientId,
            Read = DateTimeOffset.Now,
        };

        await sut.Publish(newUnreadNotification, TestContext.Current.CancellationToken);
        await sut.Publish(oldUnreadNotification, TestContext.Current.CancellationToken);
        await sut.Publish(oldReadNotification, TestContext.Current.CancellationToken);
        await sut.Publish(newReadNotification, TestContext.Current.CancellationToken);

        IReadOnlyList<Notification> notifications = await sut.GetUnreadNotifications(
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
            await context.AddAthleteAsync(new Athlete { Id = createdById }, TestContext.Current.CancellationToken);
            await context.AddAthleteAsync(new Athlete { Id = recipientId }, TestContext.Current.CancellationToken);
        }

        NewFollowRequest newUnreadNotification = new()
        {
            Created = DateTimeOffset.Now,
            CreatedById = createdById,
            RecipientId = recipientId,
        };

        NewFollowRequest newReadNotification = new()
        {
            Created = DateTimeOffset.Now,
            CreatedById = createdById,
            RecipientId = recipientId,
            Read = DateTimeOffset.Now,
        };
        NewFollowRequest oldUnreadNotification = new()
        {
            Created = DateTimeOffset.Now.AddDays(-6),
            CreatedById = createdById,
            RecipientId = recipientId,
        };
        NewFollowRequest oldReadNotification = new()
        {
            Created = DateTimeOffset.Now.AddDays(-6),
            CreatedById = createdById,
            RecipientId = recipientId,
            Read = DateTimeOffset.Now.AddDays(-6),
        };

        await sut.Publish(newUnreadNotification, TestContext.Current.CancellationToken);
        await sut.Publish(oldUnreadNotification, TestContext.Current.CancellationToken);
        await sut.Publish(oldReadNotification, TestContext.Current.CancellationToken);
        await sut.Publish(newReadNotification, TestContext.Current.CancellationToken);
        await sut.RemoveOldNotifications(recipientId, TestContext.Current.CancellationToken);

        IReadOnlyList<Notification> notifications = await sut.GetNotifications(
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
