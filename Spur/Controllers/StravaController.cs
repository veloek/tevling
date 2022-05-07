using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
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
    private readonly IAthleteRepository _athleteRepository;
    private readonly IActivityRepository _activityRepository;
    private readonly IActivityService _activityService;

    public StravaController(
        ILogger<StravaController> logger,
        StravaConfig stravaConfig,
        IAthleteRepository athleteRepository,
        IActivityRepository activityRepository,
        IActivityService activityService)
    {
        _logger = logger;
        _stravaConfig = stravaConfig;
        _athleteRepository = athleteRepository;
        _activityRepository = activityRepository;
        _activityService = activityService;
    }

    [HttpPost]
    [Route("activity")]
    public Task OnActivity([FromBody] WebhookEvent activity, CancellationToken ct)
    {
        if (activity.SubscriptionId != _stravaConfig.SubscriptionId)
            throw new Exception("Invalid subscription ID");

        var task = (activity.ObjectType, activity.AspectType) switch
        {
            ("activity", "create") => CreateActivity(activity.OwnerId, activity.ObjectId, ct),
            ("activity", "update") => UpdateActivity(activity.OwnerId, activity.ObjectId, ct),
            ("activity", "delete") => DeleteActivity(activity.OwnerId, activity.ObjectId, ct),
            // ("athlete", "create") => CreateAthlete(activity.OwnerId, activity.ObjectId, ct),
            // ("athlete", "update") => UpdateAthlete(activity.OwnerId, activity.ObjectId, ct),
            // ("athlete", "delete") => DeleteAthlete(activity.OwnerId, activity.ObjectId, ct),
            _ => LogUnknownEvent(activity),
        };

        return task;
    }

    private async Task CreateActivity(long stravaAthleteId, long stravaActivityId,
        CancellationToken ct)
    {
        var athlete = await _athleteRepository.GetAthleteByStravaIdAsync(stravaAthleteId, ct);
        if (athlete == null)
        {
            _logger.LogWarning($"Received activity for unknown athlete ID {stravaAthleteId}");
            return;
        }

        _logger.LogInformation($"Adding activity ID {stravaActivityId} for athlete {athlete.Id}");
        var activity = await _activityRepository.AddActivityAsync(athlete.Id, stravaActivityId, ct);

        _logger.LogInformation($"Fetching activity details for activity ID {stravaActivityId}");
        var activityDetails = await _activityService.FetchActivityDetailsAsync(activity, CancellationToken.None);

        activity.Details = activityDetails;
        await _activityRepository.UpdateActivityAsync(activity, CancellationToken.None);
    }

    private async Task UpdateActivity(long stravaAthleteId, long stravaActivityId,
        CancellationToken ct)
    {
        var athlete = await _athleteRepository.GetAthleteByStravaIdAsync(stravaAthleteId, ct);
        if (athlete == null)
        {
            _logger.LogWarning($"Received activity update for unknown athlete ID {stravaAthleteId}");
            return;
        }

        var activity = await _activityRepository.GetActivityAsync(athlete.Id, stravaActivityId, ct);
        if (activity == null)
        {
            _logger.LogWarning($"Received activity update for unknown activity ID {stravaActivityId}");
            return;
        }

        // TODO: Handle update
    }

    private async Task DeleteActivity(long stravaAthleteId, long stravaActivityId,
        CancellationToken ct)
    {
        var athlete = await _athleteRepository.GetAthleteByStravaIdAsync(stravaAthleteId, ct);
        if (athlete == null)
        {
            _logger.LogWarning($"Received activity update for unknown athlete ID {stravaAthleteId}");
            return;
        }

        var activity = await _activityRepository.GetActivityAsync(athlete.Id, stravaActivityId, ct);
        if (activity == null)
        {
            _logger.LogWarning($"Received activity update for unknown activity ID {stravaActivityId}");
            return;
        }

        await _activityRepository.RemoveActivityAsync(activity, ct);
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
        var httpClient = new HttpClient();

        var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string?>("client_id", _stravaConfig.ClientId.ToString()),
                new KeyValuePair<string, string?>("client_secret", _stravaConfig.ClientSecret),
                new KeyValuePair<string, string?>("grant_type", "authorization_code"),
                new KeyValuePair<string, string?>("code", code),
            });

        var response = await httpClient.PostAsync(_stravaConfig.TokenUri, content, ct);

        // TODO: Handle error properly
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync(ct);
        var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(responseBody);

        if (tokenResponse is null)
            throw new Exception("Error deserializing token response");

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
