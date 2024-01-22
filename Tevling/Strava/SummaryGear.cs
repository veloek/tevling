using System.Text.Json.Serialization;

namespace Tevling.Strava;

public class SummaryGear
{
    [JsonPropertyName("id")]
    // The gear's unique identifier.
    public string? Id { get; set; }

    [JsonPropertyName("resource_state")]
    // Resource state, indicates level of detail. Possible values: 2 -> "summary", 3 -> "detail"
    public int Resource_state { get; set; }

    [JsonPropertyName("primary")]
    // Whether this gear's is the owner's default one.
    public bool Primary { get; set; }

    [JsonPropertyName("name")]
    // The gear's name.
    public string? Name { get; set; }

    [JsonPropertyName("distance")]
    // The distance logged with this gear.
    public float Distance { get; set; }
}
