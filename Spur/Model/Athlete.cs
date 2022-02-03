namespace Spur.Model;

public class Athlete
{
    public int Id { get; set; }
    public long StravaId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ImgUrl { get; set; }
    public DateTimeOffset Created { get; set; }

    public ICollection<Activity>? Activities { get; set; }
    public ICollection<Challenge>? Challenges { get; set; }
}
