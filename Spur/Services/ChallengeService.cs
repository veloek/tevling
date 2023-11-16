using Microsoft.EntityFrameworkCore;
using Spur.Data;
using Spur.Model;

namespace Spur.Services;

public class ChallengeService : IChallengeService
{
    private readonly ILogger<ChallengeService> _logger;
    private readonly IDbContextFactory<DataContext> _dataContextFactory;

    public ChallengeService(
        ILogger<ChallengeService> logger,
        IDbContextFactory<DataContext> dataContextFactory)
    {
        _logger = logger;
        _dataContextFactory = dataContextFactory;
    }

    public async Task<IReadOnlyList<Challenge>> GetChallengesAsync(CancellationToken ct = default)
    {
        using DataContext dataContext = await _dataContextFactory.CreateDbContextAsync(ct);

        List<Challenge> challenges = await dataContext.Challenges.ToListAsync(ct);
        return challenges;
    }

    public Task<Challenge> CreateChallengeAsync(CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}
