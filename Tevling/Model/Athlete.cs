namespace Tevling.Model;

public class Athlete
{
    public int Id { get; set; }
    public long StravaId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ImgUrl { get; set; }
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTimeOffset AccessTokenExpiry { get; set; }
    public DateTimeOffset Created { get; set; }
    public bool HasImportedActivities { get; set; }

    public ICollection<Activity>? Activities { get; set; }
    public ICollection<Challenge>? Challenges { get; set; }
    public ICollection<ChallengeTemplate>? ChallengeTemplates { get; set; }
    public ICollection<Athlete>? Following { get; set; }
    public ICollection<Athlete>? Followers { get; set; }
    public ICollection<Athlete>? PendingFollowing { get; set; }
    public ICollection<Athlete>? PendingFollowers { get; set; }

    public bool IsFollowing(int athleteId)
    {
        bool? result = Following?.Select(athlete => athlete.Id).Contains(athleteId);
        return result == true;
    }

    public bool IsFollower(int athleteId)
    {
        bool? result = Followers?.Select(athlete => athlete.Id).Contains(athleteId);
        return result == true;
    }
    public bool IsPendingFollowing(int athleteId)
    {
        bool? result = PendingFollowing?.Select(athlete => athlete.Id).Contains(athleteId);
        return result == true;
    }
    public bool IsPendingFollower(int athleteId)
    {
        bool? result = PendingFollowers?.Select(athlete => athlete.Id).Contains(athleteId);
        return result == true;
    }
}
