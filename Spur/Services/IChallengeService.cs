using Spur.Model;

namespace Spur.Services;

public interface IChallengeService
{
    Task<IReadOnlyList<Challenge>> GetChallengesAsync(CancellationToken ct = default);
}
