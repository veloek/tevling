using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement.Mvc;
using RouteAttribute = Microsoft.AspNetCore.Mvc.RouteAttribute;

namespace Tevling.Controllers;

[ApiController]
[Route("challenges")]
[FeatureGate(FeatureFlag.EnableChallengeApi)]
public class ChallengeController : ControllerBase
{
    public ChallengeController()
    {
    }

    [HttpGet]
    [Route("scoreboard/{stravaId}")]
    public IActionResult GetChallenges(long stravaId)
    {
        List<AthleteScore> scores =
        [
            new AthleteScore(
                "Fjomp1",
               "23"
            ),

            new AthleteScore(
                "Fjomp2",
                "30"
            ),

        ];
        ScoreBoard scoreBoard = new
        (
            scores
        );
        return new JsonResult(scoreBoard);
    }
}
