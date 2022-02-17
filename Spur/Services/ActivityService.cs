using Spur.Model;

namespace Spur.Services;

public class ActivityService : IActivityService
{
    public Task<ActivityDetails> FetchActivityDetailsAsync(Activity activity, CancellationToken ct = default)
    {
        // TODO: Use athlete's access token / refresh token to call Strava API
        //       and get activity details
        throw new NotImplementedException();
    }
}
