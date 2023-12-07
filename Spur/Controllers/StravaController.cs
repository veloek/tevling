using Microsoft.AspNetCore.Mvc;
using Spur.Clients;
using Spur.Services;
using Spur.Strava;

namespace Spur.Controllers;

/// <summary>
/// Callback API used for authentication flow and Strava subscription.
/// (No endpoints used by webapp)
/// </summary>
[ApiController]
[Route("api")]
public class StravaController : ControllerBase
{
    private readonly ILogger<StravaController> _logger;
    private readonly StravaConfig _stravaConfig;
    private readonly IStravaClient _stravaClient;
    private readonly IAthleteService _athleteService;
    private readonly IActivityService _activityService;
    private readonly IAuthenticationService _authenticationService;

    public StravaController(
        ILogger<StravaController> logger,
        StravaConfig stravaConfig,
        IStravaClient stravaClient,
        IAthleteService athleteService,
        IActivityService activityService,
        IAuthenticationService authenticationService)
    {
        _logger = logger;
        _stravaConfig = stravaConfig;
        _stravaClient = stravaClient;
        _athleteService = athleteService;
        _activityService = activityService;
        _authenticationService = authenticationService;
    }

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
        if (mode != "subscribe" || verifyToken != _stravaConfig.VerifyToken)
            return BadRequest();

        return new JsonResult(new Dictionary<string, string>()
        {
            ["hub.challenge"] = challenge
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
        if (activity.SubscriptionId != _stravaConfig.SubscriptionId)
            throw new Exception("Invalid subscription ID");

        _logger.LogDebug("Activity: {@Activity}", activity);

        try
        {
            await ((activity.ObjectType, activity.AspectType) switch
            {
                ("activity", "create") => _activityService.CreateActivityAsync(activity.OwnerId, activity.ObjectId, ct),
                ("activity", "update") => _activityService.UpdateActivityAsync(activity.OwnerId, activity.ObjectId, ct),
                ("activity", "delete") => _activityService.DeleteActivityAsync(activity.OwnerId, activity.ObjectId, ct),
                // ("athlete", "create") => CreateAthlete(activity.OwnerId, activity.ObjectId, ct),
                ("athlete", "update") when IsDeauthorizeEvent(activity) => _athleteService.DeleteAthleteAsync(activity.OwnerId, ct),
                // ("athlete", "delete") => DeleteAthlete(activity.OwnerId, activity.ObjectId, ct),
                _ => LogUnknownEvent(activity),
            });
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error handling activity: {Message}", e.Message);
        }
    }

    private static bool IsDeauthorizeEvent(WebhookEvent @event)
        => @event.Updates?.Contains(new("authorized", "false")) == true;

    private Task LogUnknownEvent(WebhookEvent @event)
    {
        _logger.LogWarning("Received unknown event: {@Event}", @event);

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
    public async Task<IActionResult> Authorize([FromQuery] string code, [FromQuery] string? returnUrl, CancellationToken ct)
    {
        TokenResponse tokenResponse = await _stravaClient.GetAccessTokenByAuthorizationCodeAsync(code, ct);

        if (tokenResponse.Athlete != null)
        {
            Model.Athlete athlete = await _athleteService.UpsertAthleteAsync(
                stravaId: tokenResponse.Athlete.Id,
                name: $"{tokenResponse.Athlete.Firstname} {tokenResponse.Athlete.Lastname}",
                imgUrl: tokenResponse.Athlete.Profile,
                accessToken: tokenResponse.AccessToken ?? string.Empty,
                refreshToken: tokenResponse.RefreshToken ?? string.Empty,
                accessTokenExpiry: DateTimeOffset.FromUnixTimeSeconds(tokenResponse.ExpiresAt),
                ct);

            await _authenticationService.LoginAsync(athlete, ct);
        }
        else
        {
            throw new Exception("Missing athlete data");
        }

        return LocalRedirect(returnUrl ?? "/");
    }
}
