using Microsoft.EntityFrameworkCore;
using Spur.Data;
using Spur.Model;

namespace Spur.Services;

public class ChallengeService : IChallengeService
{
    private readonly ILogger<ChallengeService> _logger;
    private readonly IDataContext _dataContext;

    public ChallengeService(
        ILogger<ChallengeService> logger,
        IDataContext dataContext)
    {
        _logger = logger;
        _dataContext = dataContext;
    }

    public async Task<IReadOnlyList<Challenge>> GetChallengesAsync(CancellationToken ct = default)
    {
        List<Challenge> challenges = await _dataContext.Challenges.ToListAsync(ct);
        return challenges;
    }

    public Task<Challenge> CreateChallengeAsync(CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}
