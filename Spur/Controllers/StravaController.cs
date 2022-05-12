using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Spur.Clients;
using Spur.Data;
using Spur.Services;
using Spur.Strava;

namespace Spur.Controllers;

[ApiController]
[Route("api")]
public class StravaController : ControllerBase
{
    private readonly ILogger<StravaController> _logger;
    private readonly StravaConfig _stravaConfig;
    private readonly IStravaClient _stravaClient;
    private readonly IAthleteRepository _athleteRepository;
    private readonly IActivityRepository _activityRepository;
    private readonly IActivityService _activityService;

    public StravaController(
        ILogger<StravaController> logger,
        StravaConfig stravaConfig,
        IStravaClient stravaClient,
        IAthleteRepository athleteRepository,
        IActivityRepository activityRepository,
        IActivityService activityService)
    {
        _logger = logger;
        _stravaConfig = stravaConfig;
        _stravaClient = stravaClient;
        _athleteRepository = athleteRepository;
        _activityRepository = activityRepository;
        _activityService = activityService;
    }

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
        var json = JsonSerializer.Serialize(@event);
        _logger.LogWarning($"Received unknown event: {json}");

        return Task.CompletedTask;
    }

    [HttpGet]
    [Route("authorize")]
    public async Task<ActionResult> Authorize([FromQuery] string code, CancellationToken ct)
    {
        var tokenResponse = await _stravaClient.GetAccessTokenByAuthorizationCodeAsync(code, ct);
        await LoginAsync(tokenResponse);

        if (tokenResponse.Athlete != null)
        {
            await _athleteRepository.UpsertAthleteAsync(
                stravaId: tokenResponse.Athlete.Id,
                name: $"{tokenResponse.Athlete.Firstname} {tokenResponse.Athlete.Lastname}",
                imgUrl: tokenResponse.Athlete.Profile,
                accessToken: tokenResponse.AccessToken ?? string.Empty,
                refreshToken: tokenResponse.RefreshToken ?? string.Empty,
                accessTokenExpiry: DateTimeOffset.FromUnixTimeSeconds(tokenResponse.ExpiresAt),
                ct);
        }

        return LocalRedirect("/");
    }

    private async Task LoginAsync(TokenResponse tokenResponse)
    {
        var fullName = $"{tokenResponse.Athlete?.Firstname} {tokenResponse.Athlete?.Lastname}";
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, fullName),
            new Claim(ClaimTypes.UserData, JsonSerializer.Serialize(tokenResponse)),
        };

        var claimsIdentity = new ClaimsIdentity(
            claims, CookieAuthenticationDefaults.AuthenticationScheme);

        var authProperties = new AuthenticationProperties
        {
            AllowRefresh = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(1),
            IsPersistent = true,
            RedirectUri = "/",
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);

        _logger.LogInformation($"User {tokenResponse.Athlete?.Id} logged in at {DateTime.Now}.");
    }
}
