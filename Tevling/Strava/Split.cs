using System.Text.Json.Serialization;

namespace Tevling.Strava;

public class Split
{
    [JsonPropertyName("average_speed")]
    // The average speed of this split, in meters per second
    public float AverageSpeed { get; set; }

    [JsonPropertyName("distance")]
    // The distance of this split, in meters
    public float Distance { get; set; }

    [JsonPropertyName("elapsed_time")]
    // The elapsed time of this split, in seconds
    public int ElapsedTime { get; set; }

    [JsonPropertyName("elevation_difference")]
    // The elevation difference of this split, in meters
    public float ElevationDifference { get; set; }

    [JsonPropertyName("pace_zone")]
    // The pacing zone of this split
    public int PaceZone { get; set; }

    [JsonPropertyName("moving_time")]
    // The moving time of this split, in seconds
    public int MovingTime { get; set; }

    [JsonPropertyName("split")]
    // N/A
    public int SplitTime { get; set; }
}
