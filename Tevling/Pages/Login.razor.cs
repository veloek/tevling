namespace Tevling.Pages;

public partial class Login : ComponentBase
{
    [Inject] private StravaConfig StravaConfig { get; set; } = null!;

    [SupplyParameterFromQuery(Name = "returnUrl")]
    private string ReturnUrl { get; set; } = string.Empty;

    private string RedirectUri => $"{StravaConfig.RedirectUri}?returnUrl={Uri.EscapeDataString(ReturnUrl)}";
}
