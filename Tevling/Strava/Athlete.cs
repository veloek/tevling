using System;
using System.Text.Json.Serialization;

namespace Tevling.Strava;

public class Athlete
{
    [JsonPropertyName("id")]
    // The unique identifier of the athlete
    public long Id { get; set; }

    [JsonPropertyName("resource_state")]
    // Resource state, indicates level of detail.
    // Possible values: 1 -> "meta", 2 -> "summary", 3 -> "detail"
    public int ResourceState { get; set; }

    [JsonPropertyName("firstname")]
    // The athlete's first name.
    public string? Firstname { get; set; }

    [JsonPropertyName("lastname")]
    // The athlete's last name.
    public string? Lastname { get; set; }

    [JsonPropertyName("profile_medium")]
    // URL to a 62x62 pixel profile picture.
    public string? ProfileMedium { get; set; }

    [JsonPropertyName("profile")]
    // URL to a 124x124 pixel profile picture.
    public string? Profile { get; set; }

    [JsonPropertyName("city")]
    // The athlete's city.
    public string? City { get; set; }

    [JsonPropertyName("state")]
    // The athlete's state or geographical region.
    public string? State { get; set; }

    [JsonPropertyName("country")]
    // The athlete's country.
    public string? Country { get; set; }

    [JsonPropertyName("sex")]
    // The athlete's sex. May take one of the following values: M, F
    public string? Sex { get; set; }

    [JsonPropertyName("premium")]
    // Deprecated. Use summit field instead.
    // Whether the athlete has any Summit subscription.
    public bool Premium { get; set; }

    [JsonPropertyName("summit")]
    // Whether the athlete has any Summit subscription.
    public bool Summit { get; set; }

    [JsonPropertyName("created_at")]
    // The time at which the athlete was created.
    public DateTimeOffset CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    // The time at which the athlete was last updated.
    public DateTimeOffset UpdatedAt { get; set; }
}
