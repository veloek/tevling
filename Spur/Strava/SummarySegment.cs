using System.Text.Json.Serialization;

namespace Spur.Strava;

public class SummarySegment
{
    [JsonPropertyName("id")]
    // The unique identifier of this segment
    public long Id { get; set; }

    [JsonPropertyName("name")]
    // The name of this segment
    public string? Name { get; set; }

    [JsonPropertyName("activity_type")]
    // May take one of the following values: Ride, Run
    public string? ActivityType { get; set; }

    [JsonPropertyName("distance")]
    // The segment's distance, in meters
    public float Distance { get; set; }

    [JsonPropertyName("average_grade")]
    // The segment's average grade, in percents
    public float AverageGrade { get; set; }

    [JsonPropertyName("maximum_grade")]
    // The segments's maximum grade, in percents
    public float MaximumGrade { get; set; }

    [JsonPropertyName("elevation_high")]
    // The segments's highest elevation, in meters
    public float ElevationHigh { get; set; }

    [JsonPropertyName("elevation_low")]
    // The segments's lowest elevation, in meters
    public float ElevationLow { get; set; }

    [JsonPropertyName("start_latlng")]
    // An instance of LatLng.
    public double[]? StartLatlng { get; set; }

    [JsonPropertyName("end_latlng")]
    // An instance of LatLng.
    public double[]? EndLatlng { get; set; }

    [JsonPropertyName("climb_category")]
    // The category of the climb [0, 5]. Higher is harder ie. 5 is Hors catégorie, 0 is uncategorized in climb_category.
    public int ClimbCategory { get; set; }

    [JsonPropertyName("city")]
    // The segments's city.
    public string? City { get; set; }

    [JsonPropertyName("state")]
    // The segments's state or geographical region.
    public string? State { get; set; }

    [JsonPropertyName("country")]
    // The segment's country.
    public string? Country { get; set; }

    [JsonPropertyName("private")]
    // Whether this segment is private.
    public bool Private { get; set; }

    [JsonPropertyName("athlete_pr_effort")]
    // An instance of SummarySegmentEffort.
    public SummarySegmentEffort? AthletePrEffort { get; set; }

    [JsonPropertyName("athlete_segment_stats")]
    // An instance of SummaryPRSegmentEffort.
    public SummaryPRSegmentEffort? AthleteSegmentStats { get; set; }
}
