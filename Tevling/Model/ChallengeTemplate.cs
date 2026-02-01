using Tevling.Strava;

namespace Tevling.Model;

public class ChallengeTemplate
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ChallengeMeasurement Measurement { get; set; }
    public float? IndividualGoal { get; set; }
    public IList<ActivityType> ActivityTypes { get; set; } = [];
    public DateTimeOffset Created { get; set; }
    public bool IsPrivate { get; set; }

    public int CreatedById { get; set; }
    public Athlete? CreatedBy { get; set; }
}
