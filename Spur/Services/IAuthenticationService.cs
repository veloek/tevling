using Spur.Model;

namespace Spur.Services;

public interface IAuthenticationService
{
    Task LoginAsync(HttpContext httpContext, Athlete athlete,
        CancellationToken ct = default);
    Task<Athlete> GetCurrentAthleteAsync(CancellationToken ct = default);
}
