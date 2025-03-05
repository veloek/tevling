using System.Text.Json.Serialization;

namespace Tevling.Strava;

public class SummarySegmentEffort
{
    [JsonPropertyName("id")]
    // The unique identifier of this effort
    public long Id { get; set; }

    [JsonPropertyName("activity_id")]
    // The unique identifier of the activity related to this effort
    public long ActivityId { get; set; }

    [JsonPropertyName("elapsed_time")]
    // The effort's elapsed time
    public int ElapsedTime { get; set; }

    [JsonPropertyName("start_date")]
    // The time at which the effort was started.
    public DateTimeOffset StartDate { get; set; }

    [JsonPropertyName("start_date_local")]
    // The time at which the effort was started in the local timezone.
    public DateTimeOffset StartDateLocal { get; set; }

    [JsonPropertyName("distance")]
    // The effort's distance in meters
    public float Distance { get; set; }

    [JsonPropertyName("is_kom")]
    // Whether this effort is the current best on the leaderboard
    public bool IsKom { get; set; }
}
