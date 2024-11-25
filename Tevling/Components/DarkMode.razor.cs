using Blazored.LocalStorage;

namespace Tevling.Components;

public partial class DarkMode : ComponentBase
{
    [Inject] private ILocalStorageService LocalStorage { get; set; } = null!;
    [Parameter] public Action<string>? OnChange { get; set; }

    private bool _isDarkMode;

    private bool IsDarkMode
    {
        get => _isDarkMode;
        set
        {
            _isDarkMode = value;
            string theme = _isDarkMode ? "dark" : "light";
            _ = LocalStorage.SetThemeAsync(theme);
            OnChange?.Invoke(theme);
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            string? currentTheme = await LocalStorage.GetThemeAsync();
            _isDarkMode = currentTheme == "dark";
            StateHasChanged();
        }
    }

    private void ToggleDarkMode()
    {
        IsDarkMode = !IsDarkMode;
    }
}
