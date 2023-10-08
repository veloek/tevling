using System.Text.Json.Serialization;

namespace Spur.Strava;

public class MetaActivity
{
    [JsonPropertyName("id")]
    // The unique identifier of the activity
    public long Id { get; set; }
}
