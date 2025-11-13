namespace Tevling.Services;

public interface INotificationService
{
    public Task<int> GetNotificationCount(int athleteId);
}
