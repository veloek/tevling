using System;
using System.Text.Json.Serialization;

namespace Spur.Strava;

public class Activity
{
    [JsonPropertyName("id")]
    // The unique identifier of the activity
    public long Id { get; set; }

    [JsonPropertyName("external_id")]
    // The identifier provided at upload time
    public string? ExternalId { get; set; }

    [JsonPropertyName("upload_id")]
    // The identifier of the upload that resulted in this activity
    public long UploadId { get; set; }

    [JsonPropertyName("athlete")]
    // An instance of MetaAthlete.
    public MetaAthlete? Athlete { get; set; }

    [JsonPropertyName("name")]
    // The name of the activity
    public string? Name { get; set; }

    [JsonPropertyName("distance")]
    // The activity's distance, in meters
    public float Distance { get; set; }

    [JsonPropertyName("moving_time")]
    // The activity's moving time, in seconds
    public int MovingTime { get; set; }

    [JsonPropertyName("elapsed_time")]
    // The activity's elapsed time, in seconds
    public int ElapsedTime { get; set; }

    [JsonPropertyName("total_elevation_gain")]
    // The activity's total elevation gain.
    public float TotalElevationGain { get; set; }

    [JsonPropertyName("elev_high")]
    // The activity's highest elevation, in meters
    public float ElevHigh { get; set; }

    [JsonPropertyName("elev_low")]
    // The activity's lowest elevation, in meters
    public float ElevLow { get; set; }

    [JsonPropertyName("type")]
    // An instance of ActivityType.
    public ActivityType Type { get; set; }

    [JsonPropertyName("start_date")]
    // The time at which the activity was started.
    public DateTimeOffset StartDate { get; set; }

    [JsonPropertyName("start_date_local")]
    // The time at which the activity was started in the local timezone.
    public DateTimeOffset StartDateLocal { get; set; }

    [JsonPropertyName("timezone")]
    // The timezone of the activity
    public string? Timezone { get; set; }

    [JsonPropertyName("start_latlng")]
    // An instance of LatLng.
    public double[]? StartLatlng { get; set; }

    [JsonPropertyName("end_latlng")]
    // An instance of LatLng.
    public double[]? EndLatlng { get; set; }

    [JsonPropertyName("achievement_count")]
    // The number of achievements gained during this activity
    public int AchievementCount { get; set; }

    [JsonPropertyName("kudos_count")]
    // The number of kudos given for this activity
    public int KudosCount { get; set; }

    [JsonPropertyName("comment_count")]
    // The number of comments for this activity
    public int CommentCount { get; set; }

    [JsonPropertyName("athlete_count")]
    // The number of athletes for taking part in a group activity
    public int AthleteCount { get; set; }

    [JsonPropertyName("photo_count")]
    // The number of Instagram photos for this activity
    public int PhotoCount { get; set; }

    [JsonPropertyName("total_photo_count")]
    // The number of Instagram and Strava photos for this activity
    public int TotalPhotoCount { get; set; }

    [JsonPropertyName("map")]
    // An instance of PolylineMap.
    public PolylineMap? Map { get; set; }

    [JsonPropertyName("trainer")]
    // Whether this activity was recorded on a training machine
    public bool Trainer { get; set; }

    [JsonPropertyName("commute")]
    // Whether this activity is a commute
    public bool Commute { get; set; }

    [JsonPropertyName("manual")]
    // Whether this activity was created manually
    public bool Manual { get; set; }

    [JsonPropertyName("private")]
    // Whether this activity is private
    public bool Private { get; set; }

    [JsonPropertyName("flagged")]
    // Whether this activity is flagged
    public bool Flagged { get; set; }

    [JsonPropertyName("workout_type")]
    // The activity's workout type
    public int WorkoutType { get; set; }

    [JsonPropertyName("upload_id_str")]
    // The unique identifier of the upload in string format
    public string? UploadIdStr { get; set; }

    [JsonPropertyName("average_speed")]
    // The activity's average speed, in meters per second
    public float AverageSpeed { get; set; }

    [JsonPropertyName("max_speed")]
    // The activity's max speed, in meters per second
    public float MaxSpeed { get; set; }

    [JsonPropertyName("has_kudoed")]
    // Whether the logged-in athlete has kudoed this activity
    public bool HasKudoed { get; set; }

    [JsonPropertyName("hide_from_home")]
    // Whether the activity is muted
    public bool HideFromHome { get; set; }

    [JsonPropertyName("gear_id")]
    // The id of the gear for the activity
    public string? GearId { get; set; }

    [JsonPropertyName("kilojoules")]
    // The total work done in kilojoules during this activity. Rides only
    public float Kilojoules { get; set; }

    [JsonPropertyName("average_watts")]
    // Average power output in watts during this activity. Rides only
    public float AverageWatts { get; set; }

    [JsonPropertyName("device_watts")]
    // Whether the watts are from a power meter, false if estimated
    public bool DeviceWatts { get; set; }

    [JsonPropertyName("max_watts")]
    // Rides with power meter data only
    public int MaxWatts { get; set; }

    [JsonPropertyName("weighted_average_watts")]
    // Similar to Normalized Power. Rides with power meter data only
    public int WeightedAverageWatts { get; set; }

    [JsonPropertyName("description")]
    // The description of the activity
    public string? Description { get; set; }

    [JsonPropertyName("photos")]
    // An instance of PhotosSummary.
    public PhotosSummary? Photos { get; set; }

    [JsonPropertyName("gear")]
    // An instance of SummaryGear.
    public SummaryGear? Gear { get; set; }

    [JsonPropertyName("calories")]
    // The number of kilocalories consumed during this activity
    public float Calories { get; set; }

    [JsonPropertyName("segment_efforts")]
    // A collection of DetailedSegmentEffort objects.
    public DetailedSegmentEffort[]? SegmentEfforts { get; set; }

    [JsonPropertyName("device_name")]
    // The name of the device used to record the activity
    public string? DeviceName { get; set; }

    [JsonPropertyName("embed_token")]
    // The token used to embed a Strava activity
    public string? EmbedToken { get; set; }

    [JsonPropertyName("splits_metric")]
    // The splits of this activity in metric units (for runs)
    public Split? SplitsMetric { get; set; }

    [JsonPropertyName("splits_standard")]
    // The splits of this activity in imperial units (for runs)
    public Split? SplitsStandard { get; set; }

    [JsonPropertyName("laps")]
    // A collection of Lap objects.
    public Lap[]? Laps { get; set; }

    [JsonPropertyName("best_efforts")]
    // A collection of DetailedSegmentEffort objects.
    public DetailedSegmentEffort[]? BestEfforts { get; set; }
}
