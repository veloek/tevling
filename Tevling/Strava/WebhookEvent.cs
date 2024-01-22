using System.Text.Json.Serialization;

namespace Tevling.Strava;

public class WebhookEvent
{
    [JsonPropertyName("object_type")]
    // Always either "activity" or "athlete."
    public string? ObjectType { get; set; }

    [JsonPropertyName("object_id")]
    // For activity events, the activity's ID. For athlete events, the athlete's ID.
    public long ObjectId { get; set; }

    [JsonPropertyName("aspect_type")]
    // Always "create," "update," or "delete."
    public string? AspectType { get; set; }

    [JsonPropertyName("updates")]
    // For activity update events, keys can contain "title," "type," and "private," which
    // is always "true" (activity visibility set to Only You) or "false" (activity visibility
    // set to Followers Only or Everyone). For app deauthorization events, there is always an
    // "authorized" : "false" key-value pair.
    public Dictionary<string, string>? Updates { get; set; }

    [JsonPropertyName("owner_id")]
    // The athlete's ID.
    public long OwnerId { get; set; }

    [JsonPropertyName("subscription_id")]
    // The push subscription ID that is receiving this event.
    public int SubscriptionId { get; set; }

    [JsonPropertyName("event_time")]
    // The time that the event occurred.
    public long EventTime { get; set; }
}
