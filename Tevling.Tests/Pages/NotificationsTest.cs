using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using NSubstitute;
using Tevling.Components;
using Tevling.Data;
using Tevling.Model;
using Tevling.Services;
using Xunit;

namespace Tevling.Pages;

public class NotificationsTest : BunitContext
{
    [Fact]
    public async Task Should_display_existing_notifications()
    {
        const int athleteId = 1;
        const int createdById = 2;
        IDbContextFactory<DataContext> dataContextFactory = new InMemoryDataContextFactory();

        await using (DataContext context =
                     await dataContextFactory.CreateDbContextAsync(Xunit.TestContext.Current.CancellationToken))
        {
            await context.AddAthleteAsync(new Athlete { Id = createdById }, Xunit.TestContext.Current.CancellationToken);
            await context.AddAthleteAsync(new Athlete { Id = athleteId }, Xunit.TestContext.Current.CancellationToken);
        }

        NotificationService notificationService = new(dataContextFactory);

        IAuthenticationService authenticationService = Substitute.For<IAuthenticationService>();
        authenticationService.GetCurrentAthleteAsync(Arg.Any<CancellationToken>()).Returns(
                Task.FromResult(new Athlete { Id = athleteId }));

        Services.AddSingleton<INotificationService>(notificationService);
        Services.AddSingleton(authenticationService);
        Services.AddSingleton(_ => Substitute.For<IBrowserTime>());
        Services.AddLocalization();
        Services.AddSingleton(_ => Substitute.For<NotificationTranslator>(Services.GetService<IStringLocalizer<NotificationCard>>()!));

        for (int i = 0; i < 10; i++)
        {
            await notificationService.Publish(
                new NewFollowRequest
                {
                    Created = DateTimeOffset.Now,
                    CreatedById = createdById,
                    RecipientId = athleteId,
                },
                Xunit.TestContext.Current.CancellationToken);
        }

        IRenderedComponent<Notifications> cut = Render<Notifications>();

        IReadOnlyList<IRenderedComponent<NotificationCard>> notificationCards = cut.FindComponents<NotificationCard>();
        Assert.Equal(10, notificationCards.Count);
    }

    [Fact]
    public async Task Should_add_to_existing_notifications()
    {
        const int athleteId = 1;
        const int createdById = 2;
        IDbContextFactory<DataContext> dataContextFactory = new InMemoryDataContextFactory();

        await using (DataContext context =
                     await dataContextFactory.CreateDbContextAsync(Xunit.TestContext.Current.CancellationToken))
        {
            await context.AddAthleteAsync(new Athlete { Id = createdById }, Xunit.TestContext.Current.CancellationToken);
            await context.AddAthleteAsync(new Athlete { Id = athleteId }, Xunit.TestContext.Current.CancellationToken);
        }

        NotificationService notificationService = new(dataContextFactory);

        IAuthenticationService authenticationService = Substitute.For<IAuthenticationService>();
        authenticationService.GetCurrentAthleteAsync(Arg.Any<CancellationToken>()).Returns(
                Task.FromResult(new Athlete { Id = athleteId }));

        Services.AddSingleton<INotificationService>(notificationService);
        Services.AddSingleton(authenticationService);
        Services.AddSingleton(_ => Substitute.For<IBrowserTime>());
        Services.AddLocalization();
        Services.AddSingleton(_ => Substitute.For<NotificationTranslator>(Services.GetService<IStringLocalizer<NotificationCard>>()!));

        for (int i = 0; i < 10; i++)
        {
            await notificationService.Publish(
                new NewFollowRequest
                {
                    Created = DateTimeOffset.Now,
                    CreatedById = createdById,
                    RecipientId = athleteId,
                },
                Xunit.TestContext.Current.CancellationToken);
        }

        IRenderedComponent<Notifications> cut = Render<Notifications>();

        IReadOnlyList<IRenderedComponent<NotificationCard>> notificationCards = cut.FindComponents<NotificationCard>();
        Assert.Equal(10, notificationCards.Count);

        for (int i = 0; i < 2; i++)
        {
            await notificationService.Publish(
                new NewFollowRequest
                {
                    Created = DateTimeOffset.Now,
                    CreatedById = createdById,
                    RecipientId = athleteId,
                },
                Xunit.TestContext.Current.CancellationToken);
        }

        notificationCards = cut.FindComponents<NotificationCard>();
        Assert.Equal(12, notificationCards.Count);
    }
}
