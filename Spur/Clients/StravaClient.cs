using System.Collections.Specialized;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Web;
using Spur.Strava;

namespace Spur.Clients;

public class StravaClient : IStravaClient
{
    private readonly ILogger<StravaClient> _logger;
    private readonly StravaConfig _stravaConfig;
    private readonly HttpClient _httpClient;


    public StravaClient(
        ILogger<StravaClient> logger,
        StravaConfig stravaConfig,
        HttpClient httpClient)
    {
        _logger = logger;
        _stravaConfig = stravaConfig;
        _httpClient = httpClient;

        try
        {
            _httpClient.BaseAddress = new Uri(_stravaConfig.BaseApiUri!);
        }
        catch
        {
            string fallback = "https://www.strava.com/api/v3/";
            _logger.LogWarning("Invalid BaseApiUri: '{BaseApiUrl}', using fallback: '{Fallback}'",
                _stravaConfig.BaseApiUri, fallback);
            _httpClient.BaseAddress = new Uri(fallback);
        }
    }

    public async Task<TokenResponse> GetAccessTokenByAuthorizationCodeAsync(
        string authorizationCode, CancellationToken ct = default)
    {
        FormUrlEncodedContent content = new(new[]
            {
                new KeyValuePair<string, string?>("client_id", _stravaConfig.ClientId.ToString()),
                new KeyValuePair<string, string?>("client_secret", _stravaConfig.ClientSecret),
                new KeyValuePair<string, string?>("grant_type", "authorization_code"),
                new KeyValuePair<string, string?>("code", authorizationCode),
            });

        HttpResponseMessage response = await _httpClient.PostAsync(_stravaConfig.TokenUri, content, ct);

        // TODO: Handle error properly
        _ = response.EnsureSuccessStatusCode();

        string responseBody = await response.Content.ReadAsStringAsync(ct);
        TokenResponse tokenResponse = JsonSerializer.Deserialize<TokenResponse>(responseBody) ??
            throw new Exception("Error deserializing token response");

        return tokenResponse;
    }

    public async Task<TokenResponse> GetAccessTokenByRefreshTokenAsync(
        string refreshToken, CancellationToken ct = default)
    {
        FormUrlEncodedContent content = new(new[]
            {
                new KeyValuePair<string, string?>("client_id", _stravaConfig.ClientId.ToString()),
                new KeyValuePair<string, string?>("client_secret", _stravaConfig.ClientSecret),
                new KeyValuePair<string, string?>("grant_type", "refresh_token"),
                new KeyValuePair<string, string?>("refresh_token", refreshToken),
            });

        HttpResponseMessage response = await _httpClient.PostAsync(_stravaConfig.TokenUri, content, ct);

        // TODO: Handle error properly
        _ = response.EnsureSuccessStatusCode();

        string responseBody = await response.Content.ReadAsStringAsync(ct);
        TokenResponse tokenResponse = JsonSerializer.Deserialize<TokenResponse>(responseBody) ??
            throw new Exception("Error deserializing token response");

        return tokenResponse;
    }

    public async Task<Activity> GetActivityAsync(
        long stravaId, string accessToken, CancellationToken ct = default)
    {
        HttpRequestMessage request = new(HttpMethod.Get, $"activities/{stravaId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        HttpResponseMessage response = await _httpClient.SendAsync(request, ct);

        // TODO: Handle error properly
        _ = response.EnsureSuccessStatusCode();

        string responseBody = await response.Content.ReadAsStringAsync(ct);
        try
        {
            Activity activity = JsonSerializer.Deserialize<Activity>(responseBody) ??
                throw new Exception("Error deserializing activity");

            return activity;
        }
        catch
        {
            // This was necessary to debug some deserialization issues in prod.
            // Turns out the Strava API documentation is not 100% accurate...
            _logger.LogInformation(responseBody);
            throw;
        }
    }

    public async Task<Activity[]> GetAthleteActivitiesAsync(
        string accessToken,
        DateTimeOffset? before = null,
        DateTimeOffset? after = null,
        int? page = null,
        int? pageSize = null,
        CancellationToken ct = default)
    {
        NameValueCollection query = HttpUtility.ParseQueryString(string.Empty);
        if (before.HasValue) query["before"] = before.Value.ToUnixTimeSeconds().ToString();
        if (after.HasValue) query["after"] = after.Value.ToUnixTimeSeconds().ToString();
        if (page.HasValue) query["page"] = page.Value.ToString();
        if (pageSize.HasValue) query["per_page"] = pageSize.Value.ToString();

        HttpRequestMessage request = new(HttpMethod.Get, $"athlete/activities?{query}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        HttpResponseMessage response = await _httpClient.SendAsync(request, ct);

        // TODO: Handle error properly
        _ = response.EnsureSuccessStatusCode();

        string responseBody = await response.Content.ReadAsStringAsync(ct);
        try
        {
            Activity[] activities = JsonSerializer.Deserialize<Activity[]>(responseBody) ??
                throw new Exception("Error deserializing activities");

            return activities;
        }
        catch
        {
            // This was necessary to debug some deserialization issues in prod.
            // Turns out the Strava API documentation is not 100% accurate...
            _logger.LogInformation(responseBody);
            throw;
        }
    }

    public async Task DeauthorizeAppAsync(string accessToken, CancellationToken ct = default)
    {
        HttpRequestMessage request = new(HttpMethod.Post, _stravaConfig.DeauthorizeUri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        await _httpClient.SendAsync(request, ct);
    }
}
