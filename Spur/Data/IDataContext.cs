using Spur.Model;

namespace Spur.Data;

public interface IDataContext
{
    IQueryable<Activity> Activities { get; }
    IQueryable<Athlete> Athletes { get; }
    Task Init();
    Task<Athlete> AddAthleteAsync(Athlete athlete, CancellationToken ct = default);
}
