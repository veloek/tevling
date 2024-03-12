using Tevling.Strava;

namespace Tevling.Model;

public class Challenge
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTimeOffset Start { get; set; }
    public DateTimeOffset End { get; set; }
    public ChallengeMeasurement Measurement { get; set; }
    public IList<ActivityType> ActivityTypes { get; set; } = [];
    public DateTimeOffset Created { get; set; }
    public bool IsPrivate { get; set; }

    public int CreatedById { get; set; }
    public Athlete? CreatedBy { get; set; }
    public ICollection<Athlete> Athletes { get; set; } = [];
    public ICollection<Athlete>? InvitedAthletes { get; set; }
}
