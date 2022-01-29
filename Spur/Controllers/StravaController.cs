using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Spur.Strava;

namespace Spur.Controllers;

[ApiController]
[Route("api")]
public class StravaController : ControllerBase
{
    private readonly ILogger<StravaController> _logger;
    private readonly StravaConfig _stravaConfig;

    public StravaController(ILogger<StravaController> logger, StravaConfig stravaConfig)
    {
        _logger = logger;
        _stravaConfig = stravaConfig;
    }

    [HttpPost]
    [Route("activity")]
    public async Task OnActivity([FromBody] WebhookEvent activity)
    {

    }

    [HttpGet]
    [Route("authorize")]
    public async Task<ActionResult> Authorize([FromQuery] string code)
    {
        var httpClient = new HttpClient();

        var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string?>("client_id", _stravaConfig.ClientId),
                new KeyValuePair<string, string?>("client_secret", _stravaConfig.ClientSecret),
                new KeyValuePair<string, string?>("grant_type", "authorization_code"),
                new KeyValuePair<string, string?>("code", code),
            });

        var response = await httpClient.PostAsync(_stravaConfig.TokenUri, content);

        // TODO: Handle error properly
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();
        var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(responseBody);

        if (tokenResponse is null)
            throw new Exception("Error deserializing token response");

        await LoginAsync(tokenResponse);

        return LocalRedirect("/");
    }

    private async Task LoginAsync(TokenResponse tokenResponse)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, $"{tokenResponse.Athlete.Firstname} {tokenResponse.Athlete.Lastname}"),
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

        _logger.LogInformation($"User {tokenResponse.Athlete.Id} logged in at {DateTime.Now}.");
    }
}
