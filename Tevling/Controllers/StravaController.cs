using Microsoft.AspNetCore.Mvc;
using Tevling.Strava;
using RouteAttribute = Microsoft.AspNetCore.Mvc.RouteAttribute;

namespace Tevling.Controllers;

/// <summary>
/// Callback API used for authentication flow and Strava subscription.
/// (No endpoints used by webapp)
/// </summary>
[ApiController]
[Route("api")]
public class StravaController(
    ILogger<StravaController> logger,
    StravaConfig stravaConfig,
    IStravaClient stravaClient,
    IAthleteService athleteService,
    IActivityService activityService,
    IAuthenticationService authenticationService)
    : ControllerBase
{
    /// <summary>
    /// Endpoint used by Strava to validate subscription callback address.
    /// </summary>
    [HttpGet]
    [Route("activity")]
    public IActionResult ValidateSubscriptionCallback(
        [FromQuery(Name = "hub.mode")] string mode,
        [FromQuery(Name = "hub.challenge")] string challenge,
        [FromQuery(Name = "hub.verify_token")] string verifyToken)
    {
        if (mode != "subscribe" || verifyToken != stravaConfig.VerifyToken)
            return BadRequest();

        return new JsonResult(
            new Dictionary<string, string>()
            {
                ["hub.challenge"] = challenge,
            });
    }

    /// <summary>
    /// Callback endpoint for Strava subscription.
    /// </summary>
    /// <param name="activity"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    [HttpPost]
    [Route("activity")]
    public async Task OnActivity([FromBody] WebhookEvent activity, CancellationToken ct)
    {
        if (activity.SubscriptionId != stravaConfig.SubscriptionId)
            throw new Exception("Invalid subscription ID");

        logger.LogDebug("Activity: {@Activity}", activity);

        try
        {
            await ((activity.ObjectType, activity.AspectType) switch
            {
                ("activity", "create") => activityService.CreateActivityAsync(activity.OwnerId, activity.ObjectId, ct),
                ("activity", "update") => activityService.UpdateActivityAsync(activity.OwnerId, activity.ObjectId, ct),
                ("activity", "delete") => activityService.DeleteActivityAsync(activity.OwnerId, activity.ObjectId, ct),
                // ("athlete", "create") => CreateAthlete(activity.OwnerId, activity.ObjectId, ct),
                ("athlete", "update") when IsDeauthorizeEvent(activity) => athleteService.DeleteAthleteAsync(
                    activity.OwnerId,
                    ct),
                // ("athlete", "delete") => DeleteAthlete(activity.OwnerId, activity.ObjectId, ct),
                _ => LogUnknownEvent(activity),
            });
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error handling activity: {Message}", e.Message);
        }
    }

    private static bool IsDeauthorizeEvent(WebhookEvent @event)
    {
        return @event.Updates?.Contains(new KeyValuePair<string, string>("authorized", "false")) == true;
    }

    private Task LogUnknownEvent(WebhookEvent @event)
    {
        logger.LogWarning("Received unknown event: {@Event}", @event);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Callback endpoint for Strava authentication flow.
    ///
    /// Creates or updates athlete on login. Stores access/refresh token
    /// so we can fetch information from Strava API on the user's behalf.
    /// </summary>
    /// <param name="code"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    [HttpGet]
    [Route("authorize")]
    public async Task<IActionResult> Authorize(
        [FromQuery] string code,
        [FromQuery] string? returnUrl,
        CancellationToken ct)
    {
        TokenResponse tokenResponse = await stravaClient.GetAccessTokenByAuthorizationCodeAsync(code, ct);

        if (tokenResponse.Athlete != null)
        {
            Athlete athlete = await athleteService.UpsertAthleteAsync(
                tokenResponse.Athlete.Id,
                $"{tokenResponse.Athlete.Firstname} {tokenResponse.Athlete.Lastname}",
                tokenResponse.Athlete.Profile,
                tokenResponse.AccessToken ?? string.Empty,
                tokenResponse.RefreshToken ?? string.Empty,
                DateTimeOffset.FromUnixTimeSeconds(tokenResponse.ExpiresAt),
                ct);

            await authenticationService.LoginAsync(athlete, ct);
        }
        else
        {
            throw new Exception("Missing athlete data");
        }

        return LocalRedirect(returnUrl ?? "/");
    }
}
