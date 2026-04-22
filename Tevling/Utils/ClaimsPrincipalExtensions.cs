using System.Security.Claims;

namespace Tevling.Utils;

public static class ClaimsPrincipalExtensions
{
    extension(ClaimsPrincipal target)
    {
        public string? AthleteId =>
            target.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }
}
