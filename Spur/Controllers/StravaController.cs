using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Spur.Clients;
using Spur.Data;
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
    private readonly IAthleteRepository _athleteRepository;
    private readonly IActivityService _activityService;
    private readonly IAuthenticationService _authenticationService;

    public StravaController(
        ILogger<StravaController> logger,
        StravaConfig stravaConfig,
        IStravaClient stravaClient,
        IAthleteRepository athleteRepository,
        IActivityService activityService,
        IAuthenticationService authenticationService)
    {
        _logger = logger;
        _stravaConfig = stravaConfig;
        _stravaClient = stravaClient;
        _athleteRepository = athleteRepository;
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

        try
        {
            await ((activity.ObjectType, activity.AspectType) switch
            {
                ("activity", "create") => _activityService.CreateActivityAsync(activity.OwnerId, activity.ObjectId, ct),
                ("activity", "update") => _activityService.UpdateActivityAsync(activity.OwnerId, activity.ObjectId, ct),
                ("activity", "delete") => _activityService.DeleteActivityAsync(activity.OwnerId, activity.ObjectId, ct),
                // ("athlete", "create") => CreateAthlete(activity.OwnerId, activity.ObjectId, ct),
                // ("athlete", "update") => UpdateAthlete(activity.OwnerId, activity.ObjectId, ct),
                // ("athlete", "delete") => DeleteAthlete(activity.OwnerId, activity.ObjectId, ct),
                _ => LogUnknownEvent(activity),
            });
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error handling activity: " + e.Message);
        }
    }

    private Task LogUnknownEvent(WebhookEvent @event)
    {
        string json = JsonSerializer.Serialize(@event);
        _logger.LogWarning($"Received unknown event: {json}");

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
    public async Task<ActionResult> Authorize([FromQuery] string code, CancellationToken ct)
    {
        TokenResponse tokenResponse = await _stravaClient.GetAccessTokenByAuthorizationCodeAsync(code, ct);

        if (tokenResponse.Athlete != null)
        {
            Model.Athlete athlete = await _athleteRepository.UpsertAthleteAsync(
                stravaId: tokenResponse.Athlete.Id,
                name: $"{tokenResponse.Athlete.Firstname} {tokenResponse.Athlete.Lastname}",
                imgUrl: tokenResponse.Athlete.Profile,
                accessToken: tokenResponse.AccessToken ?? string.Empty,
                refreshToken: tokenResponse.RefreshToken ?? string.Empty,
                accessTokenExpiry: DateTimeOffset.FromUnixTimeSeconds(tokenResponse.ExpiresAt),
                ct);

            await _authenticationService.LoginAsync(HttpContext, athlete, ct);
        }
        else
        {
            throw new Exception("Missing athlete data");
        }

        return LocalRedirect("/");
    }
}
