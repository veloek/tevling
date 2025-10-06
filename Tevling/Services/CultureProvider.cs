using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Localization;

namespace Tevling.Services;

public partial class CultureProvider(IHttpContextAccessor httpContextAccessor) : IProvideCulture
{
    [GeneratedRegex(@"^tevling\.no$", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
    private static partial Regex TevlingNo();

    public string Culture
    {
        get => GetCultureFromHost() ?? GetCultureFromCookie() ?? "en";
        set => StoreCultureInCookie(value);
    }

    private string? GetCultureFromHost()
    {
        if (httpContextAccessor.HttpContext is null)
            return null;

        return TevlingNo().IsMatch(httpContextAccessor.HttpContext.Request.Host.Host) ? "no" : null;
    }

    private string? GetCultureFromCookie()
    {
        if (httpContextAccessor.HttpContext is null)
            return null;

        if (!httpContextAccessor.HttpContext.Request.Cookies.TryGetValue(CookieRequestCultureProvider.DefaultCookieName, out string? cultureCookie))
            return null;

        ProviderCultureResult? result = CookieRequestCultureProvider.ParseCookieValue(cultureCookie);
        string? culture = result?.Cultures.FirstOrDefault().Value;

        return culture;
    }

    private void StoreCultureInCookie(string culture)
    {
        httpContextAccessor.HttpContext?.Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
            new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) });
    }
}
