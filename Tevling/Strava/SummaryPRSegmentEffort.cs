using System.Text.Json.Serialization;

namespace Tevling.Strava;

public class SummaryPRSegmentEffort
{
    [JsonPropertyName("pr_activity_id")]
    // The unique identifier of the activity related to the PR effort.
    public long Pr_activity_id { get; set; }

    [JsonPropertyName("pr_elapsed_time")]
    // The elapsed time ot the PR effort.
    public int Pr_elapsed_time { get; set; }

    [JsonPropertyName("pr_date")]
    // The time at which the PR effort was started.
    public DateTimeOffset Pr_date { get; set; }

    [JsonPropertyName("effort_count")]
    // Number of efforts by the authenticated athlete on this segment.
    public int Effort_count { get; set; }
}
