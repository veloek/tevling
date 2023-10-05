using System.Net.Http.Headers;
using System.Text.Json;
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
            _logger.LogWarning($"Invalid BaseApiUri '{_stravaConfig.BaseApiUri}'. Using fallback '{fallback}'.");
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

    public async Task<Activity> GetActivityAsync(long stravaId, string accessToken, CancellationToken ct = default)
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
            _logger.LogInformation(responseBody);
            throw;
        }
    }
}
