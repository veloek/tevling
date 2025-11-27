using System.Reactive.Subjects;
using Microsoft.EntityFrameworkCore;
using Tevling.Model.Notification;

namespace Tevling.Services;

public class AthleteService(
    ILogger<AthleteService> logger,
    IDbContextFactory<DataContext> dataContextFactory,
    IStravaClient stravaClient,
    INotificationService notificationService)
    : IAthleteService
{
    private readonly Subject<FeedUpdate<Athlete>> _athleteFeed = new();

    public async Task<Athlete?> GetAthleteByIdAsync(int athleteId, CancellationToken ct = default)
    {
        await using DataContext dataContext = await dataContextFactory.CreateDbContextAsync(ct);

        Athlete? athlete = await dataContext.Athletes
            .Include(a => a.Activities)
            .Include(a => a.Challenges)
            .Include(a => a.Following)
            .Include(a => a.Followers)
            .Include(a => a.PendingFollowing)
            .Include(a => a.PendingFollowers)
            .AsSplitQuery()
            .FirstOrDefaultAsync(a => a.Id == athleteId, ct);

        return athlete;
    }

    public async Task<Athlete?> GetAthleteByStravaIdAsync(long stravaId, CancellationToken ct = default)
    {
        await using DataContext dataContext = await dataContextFactory.CreateDbContextAsync(ct);

        Athlete? athlete = await dataContext.Athletes.FirstOrDefaultAsync(a => a.StravaId == stravaId, ct);

        return athlete;
    }

    public async Task<Athlete[]> GetAthletesAsync(
        AthleteFilter? filter = null,
        Paging? paging = null,
        CancellationToken ct = default)
    {
        await using DataContext dataContext = await dataContextFactory.CreateDbContextAsync(ct);

        Athlete[] athletes = await dataContext.Athletes
            .Where(athlete => filter == null ||
                !filter.FollowedBy.HasValue ||
                athlete.Followers!.Any(f => f.Id == filter.FollowedBy.Value))
            .Where(athlete => filter == null ||
                string.IsNullOrWhiteSpace(filter.SearchText)
                // TODO: Use EF.Functions.ILike when switching to PostgreSQL
                //       to keep the search text case-insensitive
                ||
                EF.Functions.Like(athlete.Name, $"%{filter.SearchText}%"))
            .Where(athlete => filter == null || filter.In == null || filter.In.Contains(athlete.Id))
            .Where(athlete => filter == null || filter.NotIn == null || !filter.NotIn.Contains(athlete.Id))
            .OrderBy(athlete => athlete.Name)
            .ThenBy(athlete => athlete.Id)
            .If(paging != null, x => x.Skip(paging!.Value.Page * paging!.Value.PageSize), x => x)
            .If(paging != null, x => x.Take(paging!.Value.PageSize))
            .ToArrayAsync(ct);

        return athletes;
    }

    public async Task<Athlete> UpsertAthleteAsync(
        long stravaId,
        string name,
        string? imgUrl,
        string accessToken,
        string refreshToken,
        DateTimeOffset accessTokenExpiry,
        CancellationToken ct = default)
    {
        await using DataContext dataContext = await dataContextFactory.CreateDbContextAsync(ct);

        bool newAthlete = false;
        Athlete? athlete = await dataContext.Athletes
            .FirstOrDefaultAsync(a => a.StravaId == stravaId, ct);

        if (athlete == null)
        {
            newAthlete = true;
            athlete = new Athlete { StravaId = stravaId, Created = DateTimeOffset.UtcNow };
        }

        athlete.Name = name;
        athlete.ImgUrl = imgUrl;
        athlete.AccessToken = accessToken;
        athlete.RefreshToken = refreshToken;
        athlete.AccessTokenExpiry = accessTokenExpiry;

        if (newAthlete)
        {
            logger.LogInformation("Adding new athlete: {Name}", athlete.Name);

            athlete = await dataContext.AddAthleteAsync(athlete, ct);
            _athleteFeed.OnNext(new FeedUpdate<Athlete> { Item = athlete, Action = FeedAction.Create });
        }
        else
        {
            logger.LogInformation("Updating athlete: {Name}", athlete.Name);

            athlete = await dataContext.UpdateAthleteAsync(athlete, ct);
            _athleteFeed.OnNext(new FeedUpdate<Athlete> { Item = athlete, Action = FeedAction.Update });
        }

        return athlete;
    }

    public async Task<Athlete> ToggleFollowingAsync(Athlete athlete, int followingId, CancellationToken ct = default)
    {
        await using DataContext dataContext = await dataContextFactory.CreateDbContextAsync(ct);

        Following? existing = await dataContext.Following
            .Where(f => f.FollowerId == athlete.Id && f.FolloweeId == followingId)
            .FirstOrDefaultAsync(ct);

        if (existing is null)
        {
            FollowRequest? pending = await dataContext.FollowRequests
                .Where(fr => fr.FollowerId == athlete.Id && fr.FolloweeId == followingId)
                .FirstOrDefaultAsync(ct);

            if (pending is null)
            {
                FollowRequest followRequest = new()
                {
                    FollowerId = athlete.Id,
                    FolloweeId = followingId,
                };
                await dataContext.AddFollowerRequestAsync(
                    followRequest,
                    ct);
                await notificationService.Publish(
                    [
                        new Notification
                        {
                            Created = DateTimeOffset.Now,
                            CreatedById = athlete.Id,
                            Recipient = followingId,
                            Type = NotificationType.FollowRequestCreated,
                        },
                    ],
                    ct);
                logger.LogInformation("Follower request created");
            }
            else
            {
                await dataContext.RemoveFollowRequestAsync(pending, ct);
                logger.LogInformation("Follower request retracted");
            }
        }
        else
        {
            await dataContext.RemoveFollowingAsync(existing, ct);
        }

        // Get an updated version of the athlete
        athlete = await GetAthleteByIdAsync(athlete.Id, ct) ?? throw new Exception("Athlete is gone");

        return athlete;
    }

    public async Task<Athlete> RemoveFollowerAsync(Athlete athlete, int followerId, CancellationToken ct = default)
    {
        await using DataContext dataContext = await dataContextFactory.CreateDbContextAsync(ct);

        Following? existing = await dataContext.Following
            .Where(f => f.FollowerId == followerId && f.FolloweeId == athlete.Id)
            .FirstOrDefaultAsync(ct);

        if (existing is not null)
        {
            await dataContext.RemoveFollowingAsync(existing, ct);
        }

        // Get an updated version of the athlete
        athlete = await GetAthleteByIdAsync(athlete.Id, ct) ?? throw new Exception("Athlete is gone");

        return athlete;
    }

    public async Task<Athlete> AcceptFollowerAsync(Athlete athlete, int followerId, CancellationToken ct = default)
    {
        await using DataContext dataContext = await dataContextFactory.CreateDbContextAsync(ct);

        FollowRequest? pending = await dataContext.FollowRequests
            .Where(fr => fr.FollowerId == followerId && fr.FolloweeId == athlete.Id)
            .FirstOrDefaultAsync(ct);

        if (pending is not null)
        {
            await dataContext.RemoveFollowRequestAsync(pending, ct);
            await notificationService.Publish([
                new Notification
                {
                    Created = DateTimeOffset.Now,
                    CreatedById = athlete.Id,
                    Recipient = followerId,
                    Type = NotificationType.FollowRequestAccepted,
                },
            ], ct);
            logger.LogInformation("Follower request accepted");
        }

        await dataContext.AddFollowingAsync(
            new Following
            {
                FollowerId = followerId,
                FolloweeId = athlete.Id,
            },
            ct);

        athlete = await GetAthleteByIdAsync(athlete.Id, ct) ?? throw new Exception("Athlete is gone");

        return athlete;
    }

    public async Task<Athlete> DeclineFollowerAsync(Athlete athlete, int followerId, CancellationToken ct = default)
    {
        await using DataContext dataContext = await dataContextFactory.CreateDbContextAsync(ct);

        FollowRequest? pending = await dataContext.FollowRequests
            .Where(fr => fr.FollowerId == followerId && fr.FolloweeId == athlete.Id)
            .FirstOrDefaultAsync(ct);

        if (pending is not null)
        {
            await dataContext.RemoveFollowRequestAsync(pending, ct);
            logger.LogInformation("Follower request declined");
        }

        athlete = await GetAthleteByIdAsync(athlete.Id, ct) ?? throw new Exception("Athlete is gone");

        return athlete;
    }

    public async Task<Athlete> SetHasImportedActivities(int athleteId, CancellationToken ct = default)
    {
        await using DataContext dataContext = await dataContextFactory.CreateDbContextAsync(ct);

        Athlete athlete = await dataContext.Athletes
                .FirstOrDefaultAsync(a => a.Id == athleteId, ct) ??
            throw new Exception("Unknown athlete id: " + athleteId);

        athlete.HasImportedActivities = true;

        await dataContext.UpdateAthleteAsync(athlete, ct);

        return athlete;
    }

    public async Task<string> GetAccessTokenAsync(int athleteId, CancellationToken ct = default)
    {
        await using DataContext dataContext = await dataContextFactory.CreateDbContextAsync(ct);

        Athlete athlete = await dataContext.Athletes
                .FirstOrDefaultAsync(a => a.Id == athleteId, ct) ??
            throw new Exception("Unknown athlete id: " + athleteId);

        if (athlete.AccessTokenExpiry - DateTimeOffset.Now < TimeSpan.FromMinutes(1))
        {
            (string accessToken, DateTimeOffset expiry, string? refreshToken) =
                await RefreshAccessToken(athlete.RefreshToken, ct);
            athlete.AccessToken = accessToken;
            athlete.AccessTokenExpiry = expiry;
            athlete.RefreshToken = refreshToken ?? athlete.RefreshToken;
            athlete = await dataContext.UpdateAthleteAsync(athlete, ct);
        }

        return athlete.AccessToken;
    }

    private async Task<(string accessToken, DateTimeOffset expiry, string? refreshToken)> RefreshAccessToken(
        string refreshToken,
        CancellationToken ct)
    {
        Strava.TokenResponse tokenResponse = await stravaClient
            .GetAccessTokenByRefreshTokenAsync(refreshToken, ct);

        if (tokenResponse.AccessToken is null)
            throw new Exception("AccessToken is null");

        return (
            tokenResponse.AccessToken,
            DateTimeOffset.FromUnixTimeSeconds(tokenResponse.ExpiresAt),
            tokenResponse.RefreshToken
        );
    }

    public IObservable<FeedUpdate<Athlete>> GetAthleteFeed()
    {
        return _athleteFeed.AsObservable();
    }

    public async Task DeleteAthleteAsync(long stravaId, CancellationToken ct = default)
    {
        await using DataContext dataContext = await dataContextFactory.CreateDbContextAsync(ct);

        Athlete athlete = await dataContext.Athletes
                .FirstOrDefaultAsync(a => a.StravaId == stravaId, ct) ??
            throw new Exception("Unknown Strava athlete id: " + stravaId);

        logger.LogInformation("Deleting athlete: {Name}", athlete.Name);
        await dataContext.RemoveAthleteAsync(athlete, ct);
    }

    public async Task<Athlete[]> GetSuggestedAthletesToFollowAsync(int athleteId, CancellationToken ct = default)
    {
        await using DataContext dataContext = await dataContextFactory.CreateDbContextAsync(ct);

        IQueryable<int> followees = dataContext.Following
            .Where(f => f.FollowerId == athleteId)
            .Select(f => f.FolloweeId);

        IQueryable<int> followedByFollowees = dataContext.Following
            .Join(followees, following => following.FollowerId, followee => followee, (f, _) => f.FolloweeId);

        Athlete[] suggestedAthletes = await dataContext.Athletes
            .Where(a => followedByFollowees.Contains(a.Id) && !followees.Contains(a.Id) && a.Id != athleteId)
            .OrderBy(a => a.Created)
            .Take(5)
            .ToArrayAsync(ct);

        return suggestedAthletes;
    }
}
