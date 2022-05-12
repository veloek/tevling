namespace Spur.Model;

public class Activity
{
    public int Id { get; set; }
    public long StravaId { get; set; }

    public int AthleteId { get; set; }
    public Athlete Athlete { get; set; } = default!;
    public ActivityDetails Details { get; set; } = default!;
}
