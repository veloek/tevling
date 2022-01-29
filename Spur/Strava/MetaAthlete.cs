using System.Text.Json.Serialization;

namespace Spur.Strava;

public class MetaAthlete
{
    [JsonPropertyName("id")]
    // The unique identifier of the athlete
    public long Id { get; set; }
}
