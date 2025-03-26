namespace Tevling.Services;

public interface IChallengeService
{
    Task<Challenge?> GetChallengeByIdAsync(int challengeId, CancellationToken ct = default);

    Task<Challenge[]> GetChallengesAsync(
        int currentAthleteId,
        ChallengeFilter filter,
        Paging? paging = null,
        CancellationToken ct = default);

    IObservable<FeedUpdate<Challenge>> GetChallengeFeed();

    Task<Challenge> CreateChallengeAsync(ChallengeFormModel challenge, CancellationToken ct = default);

    Task<ChallengeTemplate> CreateChallengeTemplateAsync(ChallengeTemplate newChallengeTemplate,
        CancellationToken ct = default);

    Task<ChallengeTemplate[]> GetChallengeTemplatesAsync(
        int currentAthleteId,
        CancellationToken ct = default);

    Task DeleteChallengeTemplateAsync(int templateId, CancellationToken ct = default);

    Task<Challenge> UpdateChallengeAsync(
        int challengeId,
        ChallengeFormModel editChallenge,
        CancellationToken ct = default);

    Task<Challenge> JoinChallengeAsync(int athleteId, int challengeId, CancellationToken ct = default);

    Task<Challenge> LeaveChallengeAsync(int athleteId, int challengeId, CancellationToken ct = default);

    Task DeleteChallengeAsync(int challengeId, CancellationToken ct = default);

    Task<ScoreBoard> GetScoreBoardAsync(int challengeId, CancellationToken ct = default);

    Task<Athlete?> DrawChallengeWinnerAsync(int challengeId, CancellationToken ct = default);

    Task ClearChallengeWinnerAsync(int challengeId, CancellationToken ct = default);
}
