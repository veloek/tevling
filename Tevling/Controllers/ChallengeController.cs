using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement.Mvc;
using RouteAttribute = Microsoft.AspNetCore.Mvc.RouteAttribute;

namespace Tevling.Controllers;

[ApiController]
[Route("challenges")]
[FeatureGate(FeatureFlag.EnableChallengeApi)]
public class ChallengeController(IChallengeService challengeService) : ControllerBase
{
    [HttpGet]
    [Route("scoreboard/{stravaId}")]
    public async Task<IActionResult> GetChallenges(int stravaId, CancellationToken ct = default)
    {
        ChallengeFilter filter = new(
            string.Empty,
            stravaId,
            OnlyJoinedChallenges: true,
            IncludeOutdatedChallenges: false
        );

        Challenge[] challenges = await challengeService.GetChallengesAsync(stravaId, filter, null, ct);

        var output = await Task.WhenAll(
            challenges.Select(async c =>
            {
                ScoreBoard scoreboard = await challengeService.GetScoreBoardAsync(c.Id, ct);
                return new
                {
                    challengeTitle = c.Title,
                    scores = scoreboard.Scores,
                };
            })
        );

        return new JsonResult(output);
    }
}
