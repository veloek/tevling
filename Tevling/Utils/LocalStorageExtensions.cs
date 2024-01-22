using Blazored.LocalStorage;

namespace Tevling.Utils;

public static class LocalStorageExt
{
    private const string LS_KEY = "theme";

    public static ValueTask<string> GetThemeAsync(this ILocalStorageService localStorage)
    {
        return localStorage.GetItemAsync<string>(LS_KEY);
    }

    public static ValueTask SetThemeAsync(this ILocalStorageService localStorage, string theme)
    {
        return localStorage.SetItemAsync(LS_KEY, theme);
    }
}
