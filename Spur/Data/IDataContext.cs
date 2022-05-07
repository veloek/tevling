using Spur.Model;

namespace Spur.Data;

public interface IDataContext
{
    IQueryable<Activity> Activities { get; }
    IQueryable<Athlete> Athletes { get; }
    IQueryable<Challenge> Challenges { get; }
    Task InitAsync();
    Task<Athlete> AddAthleteAsync(Athlete athlete, CancellationToken ct = default);
    Task<Athlete> UpdateAthleteAsync(Athlete athlete, CancellationToken ct = default);
    Task<Athlete> RemoveAthleteAsync(Athlete athlete, CancellationToken ct = default);
    Task<Activity> AddActivityAsync(Activity activity, CancellationToken ct = default);
    Task<Activity> UpdateActivityAsync(Activity activity, CancellationToken ct = default);
    Task<Activity> RemoveActivityAsync(Activity activity, CancellationToken ct = default);
}
