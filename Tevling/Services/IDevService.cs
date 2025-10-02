using Tevling.Strava;

namespace Tevling.Services;

public interface IDevService
{
    public void SetRandomEnabled(bool changed);
    public bool IsRandomEnabled();
    
    public DetailedActivity GetActivity(long stravaId);
}
