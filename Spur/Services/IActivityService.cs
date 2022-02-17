using Spur.Model;

namespace Spur.Services;

public interface IActivityService
{
    Task<ActivityDetails> FetchActivityDetailsAsync(Activity activity, CancellationToken ct = default);
}
