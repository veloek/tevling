using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.FeatureManagement.Mvc;
using Tevling.Strava;
using RouteAttribute = Microsoft.AspNetCore.Mvc.RouteAttribute;

namespace Tevling.Controllers;

[ApiController]
[Route("dev")]
[FeatureGate(FeatureFlag.DevTools)]
public class DevController(IDevService devService) : ControllerBase
{

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
        return new JsonResult(devService.GetActivity(stravaId));
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
            },
        };

        return new JsonResult(token);
    }
}
