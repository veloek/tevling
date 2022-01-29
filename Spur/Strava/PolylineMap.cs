using System.Text.Json.Serialization;

namespace Spur.Strava;

public class PolylineMap
{
#nullable disable warnings
    [JsonPropertyName("id")]
    // The identifier of the map
    public string Id { get; set; }


    [JsonPropertyName("polyline")]
    // The polyline of the map, only returned on detailed representation of an object
    public string Polyline { get; set; }


    [JsonPropertyName("summary_polyline")]
    // The summary polyline of the map
    public string SummaryPolyline { get; set; }
#nullable restore warnings
}
