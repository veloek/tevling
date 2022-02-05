using Spur.Data;
using Spur.Model;

namespace Spur.Services;

public class ChallengeService : IChallengeService
{
    private readonly ILogger<ChallengeService> _logger;
    private readonly IChallengeRepository _challengeRepository;

    public ChallengeService(
        ILogger<ChallengeService> logger,
        IChallengeRepository challengeRepository)
    {
        _logger = logger;
        _challengeRepository = challengeRepository;
    }

    public async Task<IReadOnlyList<Challenge>> GetChallengesAsync(CancellationToken ct = default)
    {
        var challenges = await _challengeRepository.GetAllChallenges().ToListAsync(ct);
        return challenges;
    }
}
