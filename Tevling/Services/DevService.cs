using Tevling.Bogus;
using Tevling.Strava;

namespace Tevling.Services;

public class DevService : IDevService
{
    private bool _isRandomEnabled = false;

    public bool IsRandomEnabled()
    {
        return _isRandomEnabled;
    }

    public void SetRandomEnabled(bool changed)
    {
        _isRandomEnabled = changed;
    }

    public DetailedActivity GetActivity(long stravaId)
    {
        return _isRandomEnabled
            ? new DetailedActivityFaker(name: "Activity " + stravaId, stravaId: stravaId).Generate()
            : new DetailedActivity
            {
                Id = stravaId,
                Name = "Activity_" + stravaId,
                Description = "Description_" + stravaId,
                Distance = 1234,
                MovingTime = 631,
                ElapsedTime = 963,
                TotalElevationGain = 124.0f,
                Calories = 123.0f,
                Type = ActivityType.Run,
                StartDate = DateTimeOffset.UtcNow,
                Manual = true,
                DeviceName = "Garmin fēnix 6 Pro"
            };
    }
}
