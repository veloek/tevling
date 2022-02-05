using Spur.Model;

namespace Spur.Data;
public interface IChallengeRepository
{
    IAsyncEnumerable<Challenge> GetAllChallenges();
}
