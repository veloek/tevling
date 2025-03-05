using System.Text.Json.Serialization;

namespace Tevling.Strava;

public class DetailedSegmentEffort
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

    [JsonPropertyName("name")]
    // The name of the segment on which this effort was performed
    public string? Name { get; set; }

    [JsonPropertyName("activity")]
    // An instance of MetaActivity.
    public MetaActivity? Activity { get; set; }

    [JsonPropertyName("athlete")]
    // An instance of MetaAthlete.
    public MetaAthlete? Athlete { get; set; }

    [JsonPropertyName("moving_time")]
    // The effort's moving time
    public int MovingTime { get; set; }

    [JsonPropertyName("start_index")]
    // The start index of this effort in its activity's stream
    public int StartIndex { get; set; }

    [JsonPropertyName("end_index")]
    // The end index of this effort in its activity's stream
    public int EndIndex { get; set; }

    [JsonPropertyName("average_cadence")]
    // The effort's average cadence
    public float AverageCadence { get; set; }

    [JsonPropertyName("average_watts")]
    // The average wattage of this effort
    public float AverageWatts { get; set; }

    [JsonPropertyName("device_watts")]
    // For riding efforts, whether the wattage was reported by a dedicated recording device
    public bool DeviceWatts { get; set; }

    [JsonPropertyName("average_heartrate")]
    // The heart heart rate of the athlete during this effort
    public float AverageHeartrate { get; set; }

    [JsonPropertyName("max_heartrate")]
    // The maximum heart rate of the athlete during this effort
    public float MaxHeartrate { get; set; }

    [JsonPropertyName("segment")]
    // An instance of SummarySegment.
    public SummarySegment? Segment { get; set; }

    [JsonPropertyName("kom_rank")]
    // The rank of the effort on the global leaderboard if it belongs in the top 10 at the time of upload
    public int? KomRank { get; set; }

    [JsonPropertyName("pr_rank")]
    // The rank of the effort on the athlete's leaderboard if it belongs in the top 3 at the time of upload
    public int? PrRank { get; set; }

    [JsonPropertyName("hidden")]
    // Whether this effort should be hidden when viewed within an activity
    public bool Hidden { get; set; }
}
