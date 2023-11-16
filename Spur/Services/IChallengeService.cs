using Spur.Model;

namespace Spur.Services;

public interface IChallengeService
{
    Task<Challenge[]> GetChallengesForAthleteAsync(int athleteId, CancellationToken ct = default);

    IObservable<FeedUpdate<Challenge>> GetChallengeFeedForAthlete(int athleteId);

    Task<Challenge> CreateChallengeAsync(ChallengeFormModel challenge, CancellationToken ct = default);

    Task<Challenge> UpdateChallengeAsync(int challengeId, ChallengeFormModel editChallenge,
        CancellationToken ct = default);

    Task DeleteChallengeAsync(int challengeId, CancellationToken ct = default);
}
