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
        bool exists = await _dataContext.Athletes.AnyAsync(a => a.StravaId == stravaId, ct);
        return exists;
    }

    public async Task<Athlete> UpsertAthleteAsync(long stravaId, string name, string? imgUrl,
        string accessToken, string refreshToken, DateTimeOffset accessTokenExpiry,
        CancellationToken ct = default)
    {
        bool newAthlete = false;
        Athlete? athlete = await GetAthleteByStravaIdAsync(stravaId, ct);

        if (athlete == null)
        {
            newAthlete = true;
            athlete = new Athlete { StravaId = stravaId, Created = DateTimeOffset.UtcNow };
        }

        athlete.Name = name;
        athlete.ImgUrl = imgUrl;
        athlete.AccessToken = accessToken;
        athlete.RefreshToken = refreshToken;
        athlete.AccessTokenExpiry = accessTokenExpiry;

        athlete = newAthlete
            ? await _dataContext.AddAthleteAsync(athlete, ct)
            : await _dataContext.UpdateAthleteAsync(athlete, ct);

        return athlete;
    }

    public async Task<Athlete?> GetAthleteByStravaIdAsync(long stravaId,
        CancellationToken ct = default)
    {
        Athlete? athlete = await _dataContext.Athletes
            .FirstOrDefaultAsync(a => a.StravaId == stravaId, ct);

        return athlete;
    }

    public async Task<Athlete?> GetAthleteByIdAsync(long athleteId,
        CancellationToken ct = default)
    {
        Athlete? athlete = await _dataContext.Athletes
            .Include(a => a.Activities)
            .FirstOrDefaultAsync(a => a.Id == athleteId, ct);

        return athlete;
    }
}
