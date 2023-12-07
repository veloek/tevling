using Microsoft.AspNetCore.Mvc;
using Spur.Services;

namespace Spur.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthenticationService _authenticationService;
    private readonly ILogger _logger;

    public AuthController(
        IAuthenticationService authenticationService,
        ILogger<AuthController> logger)
    {
        _authenticationService = authenticationService;
        _logger = logger;
    }

    [HttpGet]
    [Route("logout")]
    public async Task<IActionResult> LogOut([FromQuery]bool? deauthorize, CancellationToken ct)
    {
        await _authenticationService.LogoutAsync(deauthorize == true, ct);

        return LocalRedirect("/");
    }
}
