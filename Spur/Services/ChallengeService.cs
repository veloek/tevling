using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Spur.Data;
using Spur.Model;

namespace Spur.Services;

public class ChallengeService : IChallengeService
{
    private readonly ILogger<ChallengeService> _logger;
    private readonly IDataContext _dataContext;

    public ChallengeService(ILogger<ChallengeService> logger, IDataContext dataContext)
    {
        _logger = logger;
        _dataContext = dataContext;
    }

    public Task<IReadOnlyList<Challenge>> GetChallengesAsync(CancellationToken ct = default)
    {
        throw new System.NotImplementedException();
    }
}
