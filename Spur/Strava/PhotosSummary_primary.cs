using System.Text.Json.Serialization;

namespace Spur.Strava;

public class PhotosSummary_primary
{
    [JsonPropertyName("id")]
    // An instance of long.
    public long Id { get; set; }

    [JsonPropertyName("source")]
    // An instance of integer.
    public int Source { get; set; }

    [JsonPropertyName("unique_id")]
    // An instance of string.
    public string? UniqueId { get; set; }

    [JsonPropertyName("urls")]
    // An instance of string.
    public string? Urls { get; set; }
}
