using System.Text.Json.Serialization;

namespace Spur.Strava;

public class TokenResponse
{
#nullable disable warnings
    [JsonPropertyName("token_type")]
    public string TokenType { get; set; }

    [JsonPropertyName("expires_at")]
    public int ExpiresAt { get; set; }

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("refresh_token")]
    public string RefreshToken { get; set; }

    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; }

    [JsonPropertyName("athlete")]
    public Athlete Athlete { get; set; }
#nullable restore warnings
}
