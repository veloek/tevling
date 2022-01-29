namespace Spur.Model;

public class Activity
{
    public int Id { get; set; }

    public int AthleteId { get; set; }
    public Athlete? Athlete { get; set; }
}
