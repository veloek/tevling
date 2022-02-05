using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Spur.Data;
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

    public StravaController(
        ILogger<StravaController> logger,
        StravaConfig stravaConfig,
        IAthleteRepository athleteRepository,
        IActivityRepository activityRepository)
    {
        _logger = logger;
        _stravaConfig = stravaConfig;
        _athleteRepository = athleteRepository;
        _activityRepository = activityRepository;
    }

    [HttpPost]
    [Route("activity")]
    public async Task OnActivity([FromBody] WebhookEvent activity, CancellationToken ct)
    {
        if (activity.SubscriptionId != _stravaConfig.SubscriptionId)
            throw new Exception("Invalid subscription ID");

        switch (activity.ObjectType)
        {
            case "activity":
                await CreateActivity(activity.OwnerId, activity.ObjectId, ct);
                break;
            case "athlete":
                // TODO
                break;
            default:
                throw new Exception("Unknown object type");
        }
    }

    private async Task CreateActivity(long stravaAthleteId, long stravaActivityId,
        CancellationToken ct)
    {
        var athlete = await _athleteRepository.GetAthleteByStravaIdAsync(stravaAthleteId, ct);
        if (athlete == null)
        {
            _logger.LogWarning($"Received activity for unknown athlete {stravaAthleteId}");
            return;
        }

        await _activityRepository.AddActivityAsync(athlete.Id, stravaActivityId, ct);

        // TODO: Fetch activity details
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
            if (!await _athleteRepository.AthleteExistsAsync(tokenResponse.Athlete.Id, ct))
            {
                await _athleteRepository.CreateAthleteAsync(
                    stravaId: tokenResponse.Athlete.Id,
                    name: $"{tokenResponse.Athlete.Firstname} {tokenResponse.Athlete.Lastname}",
                    imgUrl: tokenResponse.Athlete.Profile,
                    ct);
            }
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
