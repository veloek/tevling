using Microsoft.AspNetCore.Mvc;
using RouteAttribute = Microsoft.AspNetCore.Mvc.RouteAttribute;

namespace Tevling.Controllers;

[ApiController]
[Route("auth")]
public class AuthController(
    IAuthenticationService authenticationService,
    ILogger<AuthController> logger)
    : ControllerBase
{
    [HttpGet]
    [Route("logout")]
    public async Task<IActionResult> LogOut([FromQuery] bool? deauthorize, CancellationToken ct)
    {
        await authenticationService.LogoutAsync(deauthorize == true, ct);

        return LocalRedirect("/");
    }
}
