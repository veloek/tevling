using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.FeatureManagement.Mvc;
using Tevling.Strava;
using RouteAttribute = Microsoft.AspNetCore.Mvc.RouteAttribute;

namespace Tevling.Controllers;

[ApiController]
[Route("dev")]
[FeatureGate(FeatureFlag.DevTools)]
public class DevController : ControllerBase
{
    public DevController()
    {
    }

    [HttpGet]
    [Route("strava/authorize")]
    public IActionResult Authorize([FromQuery(Name = "redirect_uri")] string redirectUri)
    {
        string uri = QueryHelpers.AddQueryString(redirectUri, "code", "code");
        return Redirect(uri);
    }

    [HttpPost]
    [Route("strava/deauthorize")]
    public async Task<IActionResult> Deauthorize()
    {
        return Ok();
    }

    [HttpGet]
    [Route("strava/activities/{stravaId}")]
    public IActionResult GetActivity(long stravaId)
    {
        DetailedActivity activity = new()
        {
            Id = stravaId,
            Name = "Activity_" + stravaId,
            Description = "Description_" + stravaId,
            Distance = 1234,
            MovingTime = 631,
            ElapsedTime = 963,
            TotalElevationGain = 124.0f,
            Calories = 0.0f,
            Type = ActivityType.Run,
            StartDate = DateTimeOffset.UtcNow,
            Manual = true,
        };

        return new JsonResult(activity);
    }

    [HttpGet]
    [Route("strava/athlete/activities")]
    public IActionResult GetAthleteActivities(
        [FromQuery] int page = 1,
        [FromQuery(Name = "per_page")] int pageSize = 30)
    {
        if (page <= 0) return BadRequest("Invalid page");
        if (pageSize <= 0 || pageSize > 50) return BadRequest("Invalid page size");

        SummaryActivity[] activities = Enumerable.Range(1000 * page, page < 3 ? pageSize : pageSize - 1)
            .Select(
                i => new SummaryActivity()
                {
                    Id = i,
                    Name = "Activity_" + i,
                    Distance = 0.0f,
                    MovingTime = 0,
                    ElapsedTime = 0,
                    TotalElevationGain = 0.0f,
                    Type = ActivityType.Run,
                    StartDate = DateTimeOffset.UtcNow,
                    Manual = true,
                })
            .ToArray();

        return new JsonResult(activities);
    }

    [HttpGet]
    [Route("strava/athlete")]
    public IActionResult GetAuthenticatedAthlete()
    {
        SummaryAthlete athlete = new()
        {
            Id = 1337,
            Firstname = "Trainer",
            Lastname = "McTrainface",
            Profile =
                "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAOEAAADhCAMAAAAJbSJIAAAAY1BMVEUDAwP///8AAACOjo7v7+/p6elkZGSdnZ36+vr19fXc3NypqanS0tI0NDSAgICIiIhzc3PExMQmJiYSEhKysrJra2sMDAxLS0uYmJi5ubmkpKRISEhvb28dHR1bW1s5OTnW1tYYldiwAAACt0lEQVR4nO3ca3OiMBSAYXNE8YqX2mrXtu7//5WLdt1uh8CMITkndN7nM5PhLVgYAhmNAAAAAAAAAAAAAAAAAAAAAAAAgDASmXXPd9c92k+rcSyb7TmryHpf3nYzF9mifM+lUeQUPe/Tbp1Do8jHPE3f1cE+UWSZrq9W7I0TRVZJA2tn00SFQOculomJT9FPM8PfonwoBDq3MisUSfhf9H9bq0Q56QS6udF5KpLoQt9UGRW+aQW6hVHhTq3QPZkk6p2kRqep7PUC6/tTi8KpYqHJJVEqxcKJSeFYsbCgULOwKJ+nwaqWK1BOhWXPR2wX771uRoVl31tIecm7sOh/jyybrAtP/fdEJOvC5wh7IkXOhdMYhRMKlVAYPjCFWigMH5hCLRSGD0yhFgq/bfvIGyXDK3z0rZnBFXbN9C/Wvr/I4Ao754k9R3F4hV2BbjP8Qu9DiS8lhRQmROF9MwqbA1OohcL7ZhQ2B6ZQC4X3zShsDkyhFgrvm1HYHJhCLRTeN6OwOTCFWij8t92iq/D4EwrXHYG/fsK8RX2ebsoWniM4xMKu6TXvwMMrfHRgCrVQGD4whVooDB+YQi0Uhg9MoRYKwwfOujDGN9d5f/e0678ncsy6MMJiMvI778L5S78vEFve1cyo0Llx6PexN0ffEcyssN6dSbDWIfMqTMCm0Pe9bio2aypsFQtNFqiRs2JhhOtsQGH3hERcEb4RD0nsnJCIy/twPH1hqRZotCidvKsVLk0CR4pLYVktgNk5bxbTq1FgnXjQKbRbGlJ8S1jEF2OxjeBEjRXNfPPEionpb2zsfoR/Ey+JV96zPYK3xLQL7Vr+Br8St8kWan21X+z6RqRKcYs6W2azKPv1ZYun6rCaFLFMFrvTse1bbyO9Hj+1sG4CAAAAAAAAAAAAAAAAAAAAAADAMP0Bvys1OxI64E0AAAAASUVORK5CYII=",
        };

        return new JsonResult(athlete);
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
            Athlete = new SummaryAthlete()
            {
                Id = 1337,
                Firstname = "Trainer",
                Lastname = "McTrainface",
                Profile =
                    "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAOEAAADhCAMAAAAJbSJIAAAAY1BMVEUDAwP///8AAACOjo7v7+/p6elkZGSdnZ36+vr19fXc3NypqanS0tI0NDSAgICIiIhzc3PExMQmJiYSEhKysrJra2sMDAxLS0uYmJi5ubmkpKRISEhvb28dHR1bW1s5OTnW1tYYldiwAAACt0lEQVR4nO3ca3OiMBSAYXNE8YqX2mrXtu7//5WLdt1uh8CMITkndN7nM5PhLVgYAhmNAAAAAAAAAAAAAAAAAAAAAAAAgDASmXXPd9c92k+rcSyb7TmryHpf3nYzF9mifM+lUeQUPe/Tbp1Do8jHPE3f1cE+UWSZrq9W7I0TRVZJA2tn00SFQOculomJT9FPM8PfonwoBDq3MisUSfhf9H9bq0Q56QS6udF5KpLoQt9UGRW+aQW6hVHhTq3QPZkk6p2kRqep7PUC6/tTi8KpYqHJJVEqxcKJSeFYsbCgULOwKJ+nwaqWK1BOhWXPR2wX771uRoVl31tIecm7sOh/jyybrAtP/fdEJOvC5wh7IkXOhdMYhRMKlVAYPjCFWigMH5hCLRSGD0yhFgq/bfvIGyXDK3z0rZnBFXbN9C/Wvr/I4Ao754k9R3F4hV2BbjP8Qu9DiS8lhRQmROF9MwqbA1OohcL7ZhQ2B6ZQC4X3zShsDkyhFgrvm1HYHJhCLRTeN6OwOTCFWij8t92iq/D4EwrXHYG/fsK8RX2ebsoWniM4xMKu6TXvwMMrfHRgCrVQGD4whVooDB+YQi0Uhg9MoRYKwwfOujDGN9d5f/e0678ncsy6MMJiMvI778L5S78vEFve1cyo0Llx6PexN0ffEcyssN6dSbDWIfMqTMCm0Pe9bio2aypsFQtNFqiRs2JhhOtsQGH3hERcEb4RD0nsnJCIy/twPH1hqRZotCidvKsVLk0CR4pLYVktgNk5bxbTq1FgnXjQKbRbGlJ8S1jEF2OxjeBEjRXNfPPEionpb2zsfoR/Ey+JV96zPYK3xLQL7Vr+Br8St8kWan21X+z6RqRKcYs6W2azKPv1ZYun6rCaFLFMFrvTse1bbyO9Hj+1sG4CAAAAAAAAAAAAAAAAAAAAAADAMP0Bvys1OxI64E0AAAAASUVORK5CYII=",
            },
        };

        return new JsonResult(token);
    }
}
