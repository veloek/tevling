using System.Text.Json.Serialization;

namespace Tevling.Strava;

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
    // A key-value collection of photo URLs with size as key.
    public Dictionary<string, string>? Urls { get; set; }
}
