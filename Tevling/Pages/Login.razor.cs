namespace Tevling.Pages;

public partial class Login : ComponentBase
{
    [Inject] private StravaConfig StravaConfig { get; set; } = null!;
    [Inject] private IProvideCulture CultureProvider { get; set; } = null!;

    [SupplyParameterFromQuery(Name = "returnUrl")]
    private string? ReturnUrl { get; set; }

    private string RedirectUri =>
        $"{StravaConfig.RedirectUri}?returnUrl={Uri.EscapeDataString(ReturnUrl ?? string.Empty)}&culture={CultureProvider.Culture}";
}
