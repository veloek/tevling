using Microsoft.EntityFrameworkCore;
using Spur.Model;

namespace Spur.Data;

public class AthleteRepository : IAthleteRepository
{
    private readonly ILogger<AthleteRepository> _logger;
    private readonly IDataContext _dataContext;

    public AthleteRepository(
        ILogger<AthleteRepository> logger,
        IDataContext dataContext)
    {
        _logger = logger;
        _dataContext = dataContext;
    }

    public async Task<bool> AthleteExistsAsync(long stravaId, CancellationToken ct = default)
    {
        var exists = await _dataContext.Athletes.AnyAsync(a => a.StravaId == stravaId, ct);
        return exists;
    }

    public async Task<Athlete> CreateAthleteAsync(long stravaId, string name, string? imgUrl,
        CancellationToken ct = default)
    {
        var athlete = await _dataContext.AddAthleteAsync(new Athlete
        {
            StravaId = stravaId,
            Name = name,
            ImgUrl = imgUrl,
        }, ct);

        return athlete;
    }
}
