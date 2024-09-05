using System.ComponentModel.DataAnnotations;
using Tevling.Strava;

namespace Tevling.Model;

public class ChallengeFormModel
{
    [Required] [MinLength(3)] public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
    public DateTimeOffset Start { get; set; }
    public DateTimeOffset End { get; set; }
    public ChallengeMeasurement Measurement { get; set; }
    public ICollection<ActivityType> ActivityTypes { get; set; } = [];
    public bool IsPrivate { get; set; }
    public ICollection<Athlete> InvitedAthletes { get; set; } = [];
    public int CreatedBy { get; set; }
}
