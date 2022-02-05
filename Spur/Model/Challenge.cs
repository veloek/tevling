namespace Spur.Model;

public class Challenge
{
    public int Id { get; set; }

    public DateTimeOffset Start { get; set; }
    public DateTimeOffset End { get; set; }
    public DateTimeOffset Created { get; set; }

    public ICollection<Athlete>? Athletes { get; set; }
}
