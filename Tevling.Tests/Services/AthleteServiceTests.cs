using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Tevling.Clients;
using Tevling.Data;
using Tevling.Model;
using Tevling.Strava;
using Xunit;

namespace Tevling.Services;

public class AthleteServiceTests
{
    [Fact]
    public async Task GetAthletesAsync_should_return_all_athletes()
    {
        const int numAthletes = 10;

        IDbContextFactory<DataContext> dataContextFactory = new InMemoryDataContextFactory();
        await SeedData(
            dataContextFactory,
            Enumerable.Range(1, numAthletes).Select(id => new Athlete { Id = id }),
            followings: [],
            TestContext.Current.CancellationToken);

        AthleteService sut = CreateSut(dataContextFactory);
        Athlete[] athletes = await sut.GetAthletesAsync(
            filter: null,
            paging: null,
            TestContext.Current.CancellationToken);

        athletes.Length.ShouldBe(numAthletes);
    }

    [Fact]
    public async Task GetAthletesAsync_should_return_athletes_followed_by_follower()
    {
        const int numAthletes = 10;
        const int followeeId = 1;

        IDbContextFactory<DataContext> dataContextFactory = new InMemoryDataContextFactory();
        await SeedData(
            dataContextFactory,
            Enumerable.Range(1, numAthletes).Select(id => new Athlete { Id = id }),
            [
                new Following { FollowerId = followeeId, FolloweeId = 2 },
                new Following { FollowerId = followeeId, FolloweeId = 3 },
            ],
            TestContext.Current.CancellationToken);

        AthleteService sut = CreateSut(dataContextFactory);
        Athlete[] athletes = await sut.GetAthletesAsync(
            filter: new AthleteFilter { FollowedBy = followeeId },
            paging: null,
            TestContext.Current.CancellationToken);

        athletes.Select(a => a.Id).ShouldBeSubsetOf([2, 3]);
    }

    [Fact]
    public async Task GetAthletesAsync_should_return_athletes_filtered_by_name()
    {
        const int numAthletes = 10;
        const string searchText = "super";

        IDbContextFactory<DataContext> dataContextFactory = new InMemoryDataContextFactory();
        await SeedData(
            dataContextFactory,
            Enumerable.Range(1, numAthletes)
                .Select(id => new Athlete { Id = id, Name = $"SuperAthlete {id}" })
                .Concat(
                    Enumerable.Range(11, numAthletes).Select(id => new Athlete { Id = id, Name = $"Athlete {id}" })),
            followings: [],
            TestContext.Current.CancellationToken);

        AthleteService sut = CreateSut(dataContextFactory);
        Athlete[] athletes = await sut.GetAthletesAsync(
            filter: new AthleteFilter { SearchText = searchText },
            paging: null,
            TestContext.Current.CancellationToken);

        athletes.ShouldAllBe(a => a.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetAthletesAsync_should_return_athletes_contained_in_filter()
    {
        const int numAthletes = 10;
        int[] athleteIds = Enumerable.Range(1, 5).ToArray();

        IDbContextFactory<DataContext> dataContextFactory = new InMemoryDataContextFactory();
        await SeedData(
            dataContextFactory,
            Enumerable.Range(1, numAthletes).Select(id => new Athlete { Id = id }),
            followings: [],
            TestContext.Current.CancellationToken);

        AthleteService sut = CreateSut(dataContextFactory);
        Athlete[] athletes = await sut.GetAthletesAsync(
            filter: new AthleteFilter { In = athleteIds },
            paging: null,
            TestContext.Current.CancellationToken);

        athletes.Select(a => a.Id).ShouldBeSubsetOf(athleteIds);
    }

    [Fact]
    public async Task GetAthletesAsync_should_return_athletes_not_contained_in_filter()
    {
        const int numAthletes = 10;
        int[] athleteIds = Enumerable.Range(1, 5).ToArray();

        IDbContextFactory<DataContext> dataContextFactory = new InMemoryDataContextFactory();
        await SeedData(
            dataContextFactory,
            Enumerable.Range(1, numAthletes).Select(id => new Athlete { Id = id }),
            followings: [],
            TestContext.Current.CancellationToken);

        AthleteService sut = CreateSut(dataContextFactory);
        Athlete[] athletes = await sut.GetAthletesAsync(
            filter: new AthleteFilter { NotIn = athleteIds },
            paging: null,
            TestContext.Current.CancellationToken);

        athletes.Select(a => a.Id).ShouldNotContain(id => athleteIds.Contains(id));
    }

    [Fact]
    public async Task UpsertAthleteAsync_should_create_new_athlete()
    {
        const long stravaId = 123;
        const string name = "SuperAthlete";

        IDbContextFactory<DataContext> dataContextFactory = new InMemoryDataContextFactory();
        AthleteService sut = CreateSut(dataContextFactory);

        Athlete[] existingAthletes = await sut.GetAthletesAsync(ct: TestContext.Current.CancellationToken);
        existingAthletes.ShouldBeEmpty();

        await sut.UpsertAthleteAsync(
            stravaId: stravaId,
            name: name,
            imgUrl: null,
            accessToken: "",
            refreshToken: "",
            accessTokenExpiry: default,
            TestContext.Current.CancellationToken);

        existingAthletes = await sut.GetAthletesAsync(ct: TestContext.Current.CancellationToken);
        existingAthletes.Length.ShouldBe(1);
        existingAthletes[0].Id.ShouldBePositive();
    }

    [Fact]
    public async Task UpsertAthleteAsync_should_update_existing_athlete()
    {
        const long stravaId = 123;
        const string name = "SuperAthlete";
        const string updatedName = "SuperAthlete 2";

        IDbContextFactory<DataContext> dataContextFactory = new InMemoryDataContextFactory();
        AthleteService sut = CreateSut(dataContextFactory);

        Athlete athlete = await sut.UpsertAthleteAsync(
            stravaId: stravaId,
            name: name,
            imgUrl: null,
            accessToken: "",
            refreshToken: "",
            accessTokenExpiry: default,
            TestContext.Current.CancellationToken);

        await sut.UpsertAthleteAsync(
            stravaId: stravaId,
            name: updatedName,
            imgUrl: null,
            accessToken: "",
            refreshToken: "",
            accessTokenExpiry: default,
            TestContext.Current.CancellationToken);

        Athlete? updatedAthlete = await sut.GetAthleteByIdAsync(athlete.Id, TestContext.Current.CancellationToken);
        updatedAthlete.ShouldNotBeNull();
        updatedAthlete.Name.ShouldBe(updatedName);
    }

    [Fact]
    public async Task ToggleFollowingAsync_should_add_following_if_not_existing()
    {
        const int followerId = 1;
        const int followeeId = 2;

        IDbContextFactory<DataContext> dataContextFactory = new InMemoryDataContextFactory();
        await SeedData(
            dataContextFactory,
            [
                new Athlete { Id = followerId },
                new Athlete { Id = followeeId },
            ],
            followings: [],
            TestContext.Current.CancellationToken);

        AthleteService sut = CreateSut(dataContextFactory);

        Athlete? follower = await sut.GetAthleteByIdAsync(followerId, TestContext.Current.CancellationToken);
        follower.ShouldNotBeNull();
        follower.Following.ShouldBeEmpty();

        Athlete? followee = await sut.GetAthleteByIdAsync(followeeId, TestContext.Current.CancellationToken);
        followee.ShouldNotBeNull();
        followee.Followers.ShouldBeEmpty();

        follower = await sut.ToggleFollowingAsync(follower, followeeId, TestContext.Current.CancellationToken);
        followee = await sut.AcceptFollowerAsync(followee, followerId, TestContext.Current.CancellationToken);

        // Get updated athelete
        follower = await sut.GetAthleteByIdAsync(followerId, TestContext.Current.CancellationToken);

        follower.ShouldNotBeNull();
        follower.Following.ShouldNotBeEmpty();
        followee.Followers.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task ToggleFollowingAsync_should_remove_following_if_existing()
    {
        const int followerId = 1;
        const int followeeId = 2;

        IDbContextFactory<DataContext> dataContextFactory = new InMemoryDataContextFactory();
        await SeedData(
            dataContextFactory,
            [
                new Athlete { Id = followerId },
                new Athlete { Id = followeeId },
            ],
            followings:
            [
                new Following { FollowerId = followerId, FolloweeId = followeeId },
            ],
            TestContext.Current.CancellationToken);

        AthleteService sut = CreateSut(dataContextFactory);
        Athlete? follower = await sut.GetAthleteByIdAsync(followerId, TestContext.Current.CancellationToken);
        follower.ShouldNotBeNull();
        follower.Following.ShouldNotBeEmpty();

        follower = await sut.ToggleFollowingAsync(follower, followeeId, TestContext.Current.CancellationToken);
        follower.Following.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetAccessTokenAsync_should_refresh_token_if_expired()
    {
        const string updatedAccessToken = "updated";

        IDbContextFactory<DataContext> dataContextFactory = new InMemoryDataContextFactory();
        await using DataContext dataContext = await dataContextFactory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        EntityEntry<Athlete> athleteEntry = await dataContext.Athletes.AddAsync(new Athlete(), TestContext.Current.CancellationToken);
        await dataContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        IStravaClient stravaClientMock = Substitute.For<IStravaClient>();
        stravaClientMock.GetAccessTokenByRefreshTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new TokenResponse { AccessToken = updatedAccessToken });
        
        INotificationService notificationServiceMock = Substitute.For<INotificationService>();

        AthleteService sut = new(Substitute.For<ILogger<AthleteService>>(), dataContextFactory, stravaClientMock, notificationServiceMock);;
        string accessToken = await sut.GetAccessTokenAsync(athleteEntry.Entity.Id, TestContext.Current.CancellationToken);

        accessToken.ShouldBe(updatedAccessToken);
    }

    [Fact]
    public async Task GetSuggestedAthletesToFollowAsync_should_return_5_second_level_followees()
    {
        const int numSuggestedFollowees = 5;
        int[] followedByFollowees = Enumerable.Range(3, 8).ToArray();

        IDbContextFactory<DataContext> dataContextFactory = new InMemoryDataContextFactory();
        await SeedData(
            dataContextFactory,
            Enumerable.Range(1, 10).Select(id => new Athlete { Id = id }),
            [
                new Following { FollowerId = 1, FolloweeId = 2 },
                .. followedByFollowees
                    .Select(followeeId => new Following { FollowerId = 2, FolloweeId = followeeId }),
            ],
            TestContext.Current.CancellationToken);

        AthleteService sut = CreateSut(dataContextFactory);
        Athlete[] suggestedAthletes = await sut.GetSuggestedAthletesToFollowAsync(
            athleteId: 1,
            TestContext.Current.CancellationToken);

        suggestedAthletes.Select(a => a.Id).ShouldBeSubsetOf(followedByFollowees.Take(numSuggestedFollowees));
    }

    [Fact]
    public async Task GetSuggestedAthletesToFollowAsync_should_not_return_self()
    {
        const int athleteId1 = 1;
        const int athleteId2 = 2;

        IDbContextFactory<DataContext> dataContextFactory = new InMemoryDataContextFactory();
        await SeedData(
            dataContextFactory,
            [
                new Athlete { Id = athleteId1 },
                new Athlete { Id = athleteId2 }
            ],
            [
                new Following { FollowerId = athleteId1, FolloweeId = athleteId2 },
                new Following { FollowerId = athleteId2, FolloweeId = athleteId1 }
            ],
            TestContext.Current.CancellationToken);

        AthleteService sut = CreateSut(dataContextFactory);
        Athlete[] suggestedAthletes = await sut.GetSuggestedAthletesToFollowAsync(
            athleteId1,
            TestContext.Current.CancellationToken);

        suggestedAthletes.ShouldNotContain(a => a.Id == athleteId1, "Athlete 1 should not be suggested to follow themselves");
    }

    [Fact]
    public async Task GetSuggestedAthletesToFollowAsync_should_not_return_following()
    {
        const int athleteId1 = 1;
        const int athleteId2 = 2;
        const int athleteId3 = 3;

        IDbContextFactory<DataContext> dataContextFactory = new InMemoryDataContextFactory();
        await SeedData(
            dataContextFactory,
            [
                new Athlete { Id = athleteId1 },
                new Athlete { Id = athleteId2 },
                new Athlete { Id = athleteId3 },
            ],
            [
                new Following { FollowerId = athleteId1, FolloweeId = athleteId2 },
                new Following { FollowerId = athleteId1, FolloweeId = athleteId3 },
                new Following { FollowerId = athleteId2, FolloweeId = athleteId3 },
            ],
            TestContext.Current.CancellationToken);

        AthleteService sut = CreateSut(dataContextFactory);
        Athlete[] suggestedAthletes = await sut.GetSuggestedAthletesToFollowAsync(
            athleteId1,
            TestContext.Current.CancellationToken);

        suggestedAthletes.ShouldNotContain(
            a => a.Id == athleteId3,
            "Athlete 1 should not be suggested to follow already followed athlete 3");
    }

    private static async Task SeedData(
        IDbContextFactory<DataContext> dbContextFactory,
        IEnumerable<Athlete> athletes,
        IEnumerable<Following> followings,
        CancellationToken cancellationToken)
    {
        await using DataContext dataContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await dataContext.Athletes.AddRangeAsync(athletes, cancellationToken);
        await dataContext.Following.AddRangeAsync(followings, cancellationToken);
        await dataContext.SaveChangesAsync(cancellationToken);
    }

    private static AthleteService CreateSut(IDbContextFactory<DataContext> dataContextFactory)
        => new(
            logger: Substitute.For<ILogger<AthleteService>>(),
            dataContextFactory: dataContextFactory,
            stravaClient: Substitute.For<IStravaClient>(),
            notificationService: Substitute.For<INotificationService>());
}
