namespace Tevling.Model;

public class ChallengeGroup
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public DateTimeOffset Created { get; set; }

    public int CreatedById { get; set; }

    public ICollection<Athlete>? Members { get; set; }
}
