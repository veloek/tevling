using System.Text.Json.Serialization;

namespace Tevling.Strava;

public class PhotosSummary
{
    [JsonPropertyName("count")]
    // The number of photos
    public int Count { get; set; }

    [JsonPropertyName("primary")]
    // An instance of PhotosSummary_primary.
    public PhotosSummary_primary? Primary { get; set; }
}
