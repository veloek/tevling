using Microsoft.Extensions.Options;

namespace Tevling.Pages;

public partial class Login : ComponentBase
{
    [Inject] private IOptions<StravaConfig> StravaConfig { get; set; } = null!;
    [Inject] private IHttpContextAccessor HttpContextAccessor { get; set; } = null!;

    [SupplyParameterFromQuery(Name = "returnUrl")]
    private string? ReturnUrl { get; set; }

    private string RedirectUri =>
        $"{StravaConfig.Value.RedirectUri}?returnUrl={Uri.EscapeDataString(ReturnUrl ?? "")}&host={HttpContextAccessor.HttpContext?.Request.Host}";
}
