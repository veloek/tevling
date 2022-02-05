using Microsoft.EntityFrameworkCore;
using Spur.Model;

namespace Spur.Data;

public class ChallengeRepository : IChallengeRepository
{
    private readonly ILogger<ChallengeRepository> _logger;
    private readonly IDataContext _dataContext;

    public ChallengeRepository(
        ILogger<ChallengeRepository> logger,
        IDataContext dataContext)
    {
        _logger = logger;
        _dataContext = dataContext;
    }

    public IAsyncEnumerable<Challenge> GetAllChallenges()
    {
        return _dataContext.Challenges.AsAsyncEnumerable();
    }
}
