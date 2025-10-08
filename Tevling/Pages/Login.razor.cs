using Microsoft.Extensions.Options;

namespace Tevling.Pages;

public partial class Login : ComponentBase
{
    [Inject] private IOptions<StravaConfig> StravaConfig { get; set; } = null!;
    [Inject] private IHttpContextAccessor HttpContextAccessor { get; set; } = null!;

    [SupplyParameterFromQuery(Name = "returnUrl")]
    private string? ReturnUrl { get; set; }

    private string RedirectUri => $"{StravaConfig.Value.RedirectUri}?state={GetQueryState()}";

    private string GetQueryState()
    {
        QueryString queryString = QueryString.Create(
            [
                new KeyValuePair<string, string?>("returnUrl", ReturnUrl),
                new KeyValuePair<string, string?>("host", HttpContextAccessor.HttpContext?.Request.Host.ToString())
            ]);

        return queryString.ToString().ToBase64();
    }
}
