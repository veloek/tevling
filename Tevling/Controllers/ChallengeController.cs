using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement.Mvc;
using Tevling.Authentication;
using RouteAttribute = Microsoft.AspNetCore.Mvc.RouteAttribute;

namespace Tevling.Controllers;

[ApiController]
[Route("challenges")]
[FeatureGate(FeatureFlag.EnableChallengeApi)]
[Authorize(AuthenticationSchemes = StravaAuthenticationDefaults.AuthenticationScheme)]
public class ChallengeController(IChallengeService challengeService) : ControllerBase
{
    [HttpGet]
    [Route("scoreboard")]
    public async Task<IActionResult> GetChallenges(CancellationToken ct = default)
    {

        if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value, out int userId))
        {
            return StatusCode(500);
        }
        
        ChallengeFilter filter = new(
            string.Empty,
            userId,
            OnlyJoinedChallenges: true,
            IncludeOutdatedChallenges: false
        );

        Challenge[] challenges = await challengeService.GetChallengesAsync(userId, filter, null, ct);

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
