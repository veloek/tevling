namespace Tevling.Services;

public interface IAthleteService
{
    Task<Athlete?> GetAthleteByIdAsync(int athleteId, CancellationToken ct = default);

    Task<Athlete[]> GetAthletesAsync(
        AthleteFilter? filter = null,
        Paging? paging = null,
        CancellationToken ct = default);

    Task<Athlete> UpsertAthleteAsync(
        long stravaId,
        string name,
        string? imgUrl,
        string accessToken,
        string refreshToken,
        DateTimeOffset accessTokenExpiry,
        CancellationToken ct = default);

    Task<Athlete> ToggleFollowingAsync(Athlete athlete, int followingId, CancellationToken ct = default);
    Task<Athlete> RemoveFollowerAsync(Athlete athlete, int followerId, CancellationToken ct = default);

    Task<Athlete> SetHasImportedActivities(int athleteId, CancellationToken ct = default);

    Task<string> GetAccessTokenAsync(int athleteId, CancellationToken ct = default);

    IObservable<FeedUpdate<Athlete>> GetAthleteFeed();

    Task DeleteAthleteAsync(long stravaId, CancellationToken ct = default);
    Task<Athlete[]> GetSuggestedAthletesToFollowAsync(int athleteId, CancellationToken ct = default);
}
