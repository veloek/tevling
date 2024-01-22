namespace Tevling.Services;

public interface IAuthenticationService
{
    Task LoginAsync(Athlete athlete, CancellationToken ct = default);
    Task<Athlete> GetCurrentAthleteAsync(CancellationToken ct = default);
    Task LogoutAsync(bool deauthorizeApp = false, CancellationToken ct = default);
}
