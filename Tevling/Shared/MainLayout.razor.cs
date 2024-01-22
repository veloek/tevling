namespace Tevling.Shared;

public partial class MainLayout : LayoutComponentBase
{
    [Inject]
    IAuthenticationService AuthenticationService { get; set; } = null!;

    [Inject]
    Blazored.LocalStorage.ILocalStorageService LocalStorage { get; set; } = null!;

    private Athlete Athlete { get; set; } = default!;
    private string? Theme { get; set; }
    private bool ThemeSet { get; set; }

    protected override async Task OnInitializedAsync()
    {
        Athlete = await AuthenticationService.GetCurrentAthleteAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            Theme = await LocalStorage.GetThemeAsync();
            ThemeSet = true;
            StateHasChanged();
        }
    }

    private void OnThemeChange(string newTheme)
    {
        Theme = newTheme;
        StateHasChanged();
    }
}
