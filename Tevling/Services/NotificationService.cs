namespace Tevling.Services;

public class NotificationService(IAthleteService athleteService, IChallengeService challengeService)
    : INotificationService
{
    public async Task<int> GetNotificationCount(int athleteId)
    {
        Athlete? athlete = await athleteService.GetAthleteByIdAsync(athleteId);

        return athlete is { PendingFollowers: not null } ? athlete.PendingFollowers.Count : 0;
    }
}
