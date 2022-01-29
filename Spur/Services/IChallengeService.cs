using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Spur.Model;

namespace Spur.Services;

public interface IChallengeService
{
    Task<IReadOnlyList<Challenge>> GetChallengesAsync(CancellationToken ct = default);
}
