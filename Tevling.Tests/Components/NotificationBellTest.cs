using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Bunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tevling.Data;
using Tevling.Model;
using Tevling.Services;

namespace Tevling.Components;

public class NotificationBellTest : BunitContext
{
    [Fact]
    public async Task New_Notification_Should_Be_Displayed()
    {
        const int athleteId = 1;
        const int createdById = 2;
        IDbContextFactory<DataContext> dataContextFactory = new InMemoryDataContextFactory();

        await using (DataContext context =
                     await dataContextFactory.CreateDbContextAsync(Xunit.TestContext.Current.CancellationToken))
        {
            context.Athletes.Add(new Athlete { Id = createdById });
            await context.SaveChangesAsync(Xunit.TestContext.Current.CancellationToken);
        }

        NotificationService notificationService = new(dataContextFactory);

        Services.AddSingleton<INotificationService>(notificationService);
        IRenderedComponent<NotificationBell> cut = Render<NotificationBell>(parameters => parameters
            .Add(p => p.AthleteId, athleteId)
            .Add(p => p.Text, "Test")
        );

        IReadOnlyList<IRenderedComponent<Badge>> badges = cut.FindComponents<Badge>();
        Assert.Empty(badges);

        await notificationService.Publish(
            new NewFollowRequest
            {
                Created = DateTimeOffset.Now,
                CreatedById = createdById,
                RecipientId = athleteId,
            },
            Xunit.TestContext.Current.CancellationToken);

        badges = cut.FindComponents<Badge>();
        Assert.Single(badges);
        Assert.Equal("1", badges[0].Instance.Text);
    }

    [Fact]
    public async Task Existing_Notifications_Should_Be_Displayed()
    {
        const int athleteId = 1;
        const int createdById = 2;
        IDbContextFactory<DataContext> dataContextFactory = new InMemoryDataContextFactory();

        await using (DataContext context =
                     await dataContextFactory.CreateDbContextAsync(Xunit.TestContext.Current.CancellationToken))
        {
            context.Athletes.Add(new Athlete { Id = createdById });
            await context.SaveChangesAsync(Xunit.TestContext.Current.CancellationToken);
        }

        NotificationService notificationService = new(dataContextFactory);
        Services.AddSingleton<INotificationService>(notificationService);

        await notificationService.Publish(
            new NewFollowRequest
            {
                Created = DateTimeOffset.Now,
                CreatedById = createdById,
                RecipientId = athleteId,
            },
            Xunit.TestContext.Current.CancellationToken);

        IRenderedComponent<NotificationBell> cut = Render<NotificationBell>(parameters => parameters
            .Add(p => p.AthleteId, athleteId)
            .Add(p => p.Text, "Test")
        );

        IReadOnlyList<IRenderedComponent<Badge>> badges = cut.FindComponents<Badge>();
        Assert.Single(badges);
        Assert.Equal("1", badges[0].Instance.Text);
    }

    [Fact]
    public async Task New_Notifications_Should_Be_Displayed_With_Correct_Count()
    {
        const int athleteId = 1;
        const int createdById = 2;
        IDbContextFactory<DataContext> dataContextFactory = new InMemoryDataContextFactory();

        await using (DataContext context =
                     await dataContextFactory.CreateDbContextAsync(Xunit.TestContext.Current.CancellationToken))
        {
            context.Athletes.Add(new Athlete { Id = createdById });
            await context.SaveChangesAsync(Xunit.TestContext.Current.CancellationToken);
        }

        NotificationService notificationService = new(dataContextFactory);

        Services.AddSingleton<INotificationService>(notificationService);
        IRenderedComponent<NotificationBell> cut = Render<NotificationBell>(parameters => parameters
            .Add(p => p.AthleteId, athleteId)
            .Add(p => p.Text, "Test")
        );

        IReadOnlyList<IRenderedComponent<Badge>> badges = cut.FindComponents<Badge>();
        Assert.Empty(badges);


        await notificationService.Publish(
            new NewFollowRequest
            {
                Created = DateTimeOffset.Now,
                CreatedById = createdById,
                RecipientId = athleteId,
            },
            Xunit.TestContext.Current.CancellationToken);

        badges = cut.FindComponents<Badge>();
        Assert.Single(badges);
        Assert.Equal("1", badges[0].Instance.Text);

        await notificationService.Publish(
            new NewFollowRequest
            {
                Created = DateTimeOffset.Now,
                CreatedById = createdById,
                RecipientId = athleteId,
            },
            Xunit.TestContext.Current.CancellationToken);

        badges = cut.FindComponents<Badge>();
        Assert.Single(badges);
        Assert.Equal("2", badges[0].Instance.Text);
    }

    [Fact]
    public async Task Ten_or_more_Notifications_Should_Display_Plus()
    {
        const int athleteId = 1;
        const int createdById = 2;
        IDbContextFactory<DataContext> dataContextFactory = new InMemoryDataContextFactory();

        await using (DataContext context =
                     await dataContextFactory.CreateDbContextAsync(Xunit.TestContext.Current.CancellationToken))
        {
            context.Athletes.Add(new Athlete { Id = createdById });
            await context.SaveChangesAsync(Xunit.TestContext.Current.CancellationToken);
        }

        NotificationService notificationService = new(dataContextFactory);

        Services.AddSingleton<INotificationService>(notificationService);
        IRenderedComponent<NotificationBell> cut = Render<NotificationBell>(parameters => parameters
            .Add(p => p.AthleteId, athleteId)
            .Add(p => p.Text, "Test")
        );

        IReadOnlyList<IRenderedComponent<Badge>> badges = cut.FindComponents<Badge>();
        Assert.Empty(badges);

        await Task.WhenAll(Enumerable.Range(0, 10).Select(_ =>
            notificationService.Publish(
                new NewFollowRequest
                {
                    Created = DateTimeOffset.Now,
                    CreatedById = createdById,
                    RecipientId = athleteId,
                },
                Xunit.TestContext.Current.CancellationToken)));

        badges = cut.FindComponents<Badge>();
        Assert.Single(badges);
        Assert.Equal("+", badges[0].Instance.Text);
    }

    [Fact]
    public async Task Read_Notifications_Should_Clear_Count()
    {
        const int athleteId = 1;
        const int createdById = 2;
        IDbContextFactory<DataContext> dataContextFactory = new InMemoryDataContextFactory();

        await using (DataContext context =
                     await dataContextFactory.CreateDbContextAsync(Xunit.TestContext.Current.CancellationToken))
        {
            context.Athletes.Add(new Athlete { Id = createdById });
            context.Athletes.Add(new Athlete { Id = athleteId });
            await context.SaveChangesAsync(Xunit.TestContext.Current.CancellationToken);
        }

        NotificationService notificationService = new(dataContextFactory);

        Services.AddSingleton<INotificationService>(notificationService);
        IRenderedComponent<NotificationBell> cut = Render<NotificationBell>(parameters => parameters
            .Add(p => p.AthleteId, athleteId)
            .Add(p => p.Text, "Test")
        );

        IReadOnlyList<IRenderedComponent<Badge>> badges = cut.FindComponents<Badge>();
        Assert.Empty(badges);

        NewFollowRequest firstNotification = await notificationService.Publish(
            new NewFollowRequest
            {
                Created = DateTimeOffset.Now,
                CreatedById = createdById,
                RecipientId = athleteId,
            },
            Xunit.TestContext.Current.CancellationToken);

        await notificationService.Publish(
            new ChallengeInvite
            {
                Created = DateTimeOffset.Now,
                CreatedById = createdById,
                RecipientId = athleteId,
            },
            Xunit.TestContext.Current.CancellationToken);

        badges = cut.FindComponents<Badge>();
        Assert.Single(badges);
        Assert.Equal("2", badges[0].Instance.Text);

        await notificationService.MarkNotificationAsRead(
            firstNotification,
            Xunit.TestContext.Current.CancellationToken);

        badges = cut.FindComponents<Badge>();
        Assert.Single(badges);
        Assert.Equal("1", badges[0].Instance.Text);
    }
}
