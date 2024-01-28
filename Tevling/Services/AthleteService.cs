using System.Reactive.Subjects;
using Microsoft.EntityFrameworkCore;

namespace Tevling.Services;

public class AthleteService : IAthleteService
{
    private readonly ILogger<AthleteService> _logger;
    private readonly IDbContextFactory<DataContext> _dataContextFactory;
    private readonly IStravaClient _stravaClient;
    private readonly Subject<FeedUpdate<Athlete>> _athleteFeed = new();

    public AthleteService(
        ILogger<AthleteService> logger,
        IDbContextFactory<DataContext> dataContextFactory,
        IStravaClient stravaClient)
    {
        _logger = logger;
        _dataContextFactory = dataContextFactory;
        _stravaClient = stravaClient;
    }

    public async Task<Athlete?> GetAthleteByIdAsync(int athleteId, CancellationToken ct = default)
    {
        using DataContext dataContext = await _dataContextFactory.CreateDbContextAsync(ct);

        Athlete? athlete = await dataContext.Athletes
            .Include(a => a.Activities)
            .Include(a => a.Challenges)
            .Include(a => a.Following)
            .Include(a => a.Followers)
            .AsSplitQuery()
            .FirstOrDefaultAsync(a => a.Id == athleteId, ct);

        return athlete;
    }

    public async Task<Athlete[]> GetAthletesAsync(AthleteFilter? filter = null, Paging? paging = null,
        CancellationToken ct = default)
    {
        using DataContext dataContext = await _dataContextFactory.CreateDbContextAsync(ct);

        Athlete[] athletes = await dataContext.Athletes
            .Where(athlete => filter == null
                || !filter.FollowedBy.HasValue
                || athlete.Followers!.Any(f => f.Id == filter.FollowedBy.Value))
            .Where(athlete => filter == null
                || string.IsNullOrWhiteSpace(filter.SearchText)
                // TODO: Use EF.Functions.ILike when switching to PostgreSQL
                //       to keep the search text case-insensitive
                || EF.Functions.Like(athlete.Name, $"%{filter.SearchText}%"))
            .Where(athlete => filter == null
                || filter.In == null
                || filter.In.Contains(athlete.Id))
            .Where(athlete => filter == null
                || filter.NotIn == null
                || !filter.NotIn.Contains(athlete.Id))
            .OrderBy(athlete => athlete.Name)
            .ThenBy(athlete => athlete.Id)
            .If(paging != null, x => x.Skip(paging!.Value.Page * paging!.Value.PageSize), x => (IQueryable<Athlete>)x)
            .If(paging != null, x => x.Take(paging!.Value.PageSize))
            .ToArrayAsync(ct);

        return athletes;
    }

    public async Task<Athlete> UpsertAthleteAsync(long stravaId, string name, string? imgUrl,
        string accessToken, string refreshToken, DateTimeOffset accessTokenExpiry,
        CancellationToken ct = default)
    {
        using DataContext dataContext = await _dataContextFactory.CreateDbContextAsync(ct);

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
            _logger.LogInformation("Adding new athlete: {Name}", athlete.Name);

            athlete = await dataContext.AddAthleteAsync(athlete, ct);
            _athleteFeed.OnNext(new FeedUpdate<Athlete> { Item = athlete, Action = FeedAction.Create });
        }
        else
        {
            _logger.LogInformation("Updating athlete: {Name}", athlete.Name);

            athlete = await dataContext.UpdateAthleteAsync(athlete, ct);
            _athleteFeed.OnNext(new FeedUpdate<Athlete> { Item = athlete, Action = FeedAction.Update });
        }

        return athlete;
    }

    public async Task<Athlete> ToggleFollowingAsync(Athlete athlete, int followingId, CancellationToken ct = default)
    {
        using DataContext dataContext = await _dataContextFactory.CreateDbContextAsync(ct);

        Following? existing = await dataContext.Following
            .Where(f => f.FollowerId == athlete.Id && f.FolloweeId == followingId)
            .FirstOrDefaultAsync(ct);

        if (existing is null)
        {
            await dataContext.AddFollowingAsync(new Following
            {
                FollowerId = athlete.Id,
                FolloweeId = followingId,
            }, ct);
        }
        else
        {
            await dataContext.RemoveFollowingAsync(existing, ct);
        }

        // Get an updated version of the athlete
        athlete = await GetAthleteByIdAsync(athlete.Id, ct)
            ?? throw new Exception("Athlete is gone");

        return athlete;
    }

    public async Task<Athlete> SetHasImportedActivities(int athleteId, CancellationToken ct = default)
    {
        using DataContext dataContext = await _dataContextFactory.CreateDbContextAsync(ct);

        Athlete athlete = await dataContext.Athletes
            .FirstOrDefaultAsync(a => a.Id == athleteId, ct) ??
            throw new Exception("Unknown athlete id: " + athleteId);

        athlete.HasImportedActivities = true;

        await dataContext.UpdateAthleteAsync(athlete, ct);

        return athlete;
    }

    public async Task<string> GetAccessTokenAsync(int athleteId, CancellationToken ct = default)
    {
        using DataContext dataContext = await _dataContextFactory.CreateDbContextAsync(ct);

        Athlete athlete = await dataContext.Athletes
            .FirstOrDefaultAsync(a => a.Id == athleteId, ct) ??
            throw new Exception("Unknown athlete id: " + athleteId);

        if ((athlete.AccessTokenExpiry - DateTimeOffset.Now) < TimeSpan.FromMinutes(1))
        {
            (string accessToken, DateTimeOffset expiry) = await RefreshAccessToken(athlete.RefreshToken, ct);
            athlete.AccessToken = accessToken;
            athlete.AccessTokenExpiry = expiry;
            athlete = await dataContext.UpdateAthleteAsync(athlete, ct);
        }

        return athlete.AccessToken;
    }

    private async Task<(string accessToken, DateTimeOffset expiry)> RefreshAccessToken(
        string refreshToken, CancellationToken ct)
    {
        Strava.TokenResponse tokenResponse = await _stravaClient
            .GetAccessTokenByRefreshTokenAsync(refreshToken, ct);

        if (tokenResponse.AccessToken is null)
            throw new Exception("AccessToken is null");

        return (tokenResponse.AccessToken, DateTimeOffset.FromUnixTimeSeconds(tokenResponse.ExpiresAt));
    }

    public IObservable<FeedUpdate<Athlete>> GetAthleteFeed()
    {
        return _athleteFeed.AsObservable();
    }

    public async Task DeleteAthleteAsync(long stravaId, CancellationToken ct = default)
    {
        using DataContext dataContext = await _dataContextFactory.CreateDbContextAsync(ct);

        Athlete athlete = await dataContext.Athletes
            .FirstOrDefaultAsync(a => a.StravaId == stravaId, ct) ??
            throw new Exception("Unknown Strava athlete id: " + stravaId);

        await dataContext.RemoveAthleteAsync(athlete, ct);
    }
}
