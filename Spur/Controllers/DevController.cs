using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement.Mvc;
using Spur.Strava;

namespace Spur.Controllers;

[ApiController]
[Route("dev")]
[FeatureGate("DevController")]
public class DevController : ControllerBase
{
    public DevController()
    {

    }

    [HttpGet]
    [Route("strava/authorize")]
    public IActionResult Authorize([FromQuery]string redirect_uri)
    {
        return Redirect(redirect_uri + "?code=code");
    }

    [HttpGet]
    [Route("strava/activities/{stravaId}")]
    public IActionResult GetActivity(long stravaId)
    {
        Activity activity = new()
        {
            Id = stravaId,
            Name = "Activity_" + stravaId,
            Description = "Description_" + stravaId,
            Distance = 0.0f,
            MovingTime = 0,
            ElapsedTime = 0,
            TotalElevationGain = 0.0f,
            Calories = 0.0f,
            Type = ActivityType.Run,
            StartDate = DateTimeOffset.Now,
            Manual = true,
        };

        return new JsonResult(activity);
    }

    [HttpPost]
    [Route("strava/token")]
    public IActionResult StravaTokenEndpoint()
    {
        TokenResponse token = new()
        {
            TokenType = "Bearer",
            ExpiresAt = (int)DateTimeOffset.Now.ToUnixTimeSeconds() + 21600,
            ExpiresIn = 21600,
            RefreshToken = "REFRESHTOKEN",
            AccessToken = "ACCESSTOKEN",
            Athlete = new Athlete
            {
                Id = 1337,
                Firstname = "Trainer",
                Lastname = "McTrainface",
            }
        };

        return new JsonResult(token);
    }
}
