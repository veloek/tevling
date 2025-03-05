using System.Text.Json.Serialization;

namespace Tevling.Strava;

public class Lap
{
    [JsonPropertyName("id")]
    // The unique identifier of this lap
    public long Id { get; set; }

    [JsonPropertyName("activity")]
    // An instance of MetaActivity.
    public MetaActivity? Activity { get; set; }

    [JsonPropertyName("athlete")]
    // An instance of MetaAthlete.
    public MetaAthlete? Athlete { get; set; }

    [JsonPropertyName("average_cadence")]
    // The lap's average cadence
    public float AverageCadence { get; set; }

    [JsonPropertyName("average_speed")]
    // The lap's average speed
    public float AverageSpeed { get; set; }

    [JsonPropertyName("distance")]
    // The lap's distance, in meters
    public float Distance { get; set; }

    [JsonPropertyName("elapsed_time")]
    // The lap's elapsed time, in seconds
    public int ElapsedTime { get; set; }

    [JsonPropertyName("start_index")]
    // The start index of this effort in its activity's stream
    public int StartIndex { get; set; }

    [JsonPropertyName("end_index")]
    // The end index of this effort in its activity's stream
    public int EndIndex { get; set; }

    [JsonPropertyName("lap_index")]
    // The index of this lap in the activity it belongs to
    public int LapIndex { get; set; }

    [JsonPropertyName("max_speed")]
    // The maximum speed of this lat, in meters per second
    public float MaxSpeed { get; set; }

    [JsonPropertyName("moving_time")]
    // The lap's moving time, in seconds
    public int MovingTime { get; set; }

    [JsonPropertyName("name")]
    // The name of the lap
    public string? Name { get; set; }

    [JsonPropertyName("pace_zone")]
    // The athlete's pace zone during this lap
    public int PaceZone { get; set; }

    [JsonPropertyName("split")]
    // An instance of integer.
    public int Split { get; set; }

    [JsonPropertyName("start_date")]
    // The time at which the lap was started.
    public DateTimeOffset StartDate { get; set; }

    [JsonPropertyName("start_date_local")]
    // The time at which the lap was started in the local timezone.
    public DateTimeOffset StartDateLocal { get; set; }

    [JsonPropertyName("total_elevation_gain")]
    // The elevation gain of this lap, in meters
    public float TotalElevationGain { get; set; }
}
