using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;

namespace Tevling.Shared;

public partial class MainLayout : LayoutComponentBase, IAsyncDisposable
{
    [Inject] private Blazored.LocalStorage.ILocalStorageService LocalStorage { get; set; } = null!;
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;
    [Inject] private IJSRuntime JsRuntime { get; set; } = null!;
    private ElementReference MainElement { get; set; }

    private IJSObjectReference? _scrollTopModule;
    private IDisposable? _locationChangingHandler;
    private string? Theme { get; set; }
    private bool ThemeSet { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            Theme = await LocalStorage.GetThemeAsync();
            ThemeSet = true;
            StateHasChanged();

            _scrollTopModule = await JsRuntime.InvokeAsync<IJSObjectReference>("import", "./scroll-top.js");
            _locationChangingHandler = NavigationManager.RegisterLocationChangingHandler(ResetScroll);
        }
    }

    private async ValueTask ResetScroll(LocationChangingContext context)
    {
        // Scroll to top on internal navigation.
        // A workaround for Blazor's helpful behavior of keeping scroll position
        // when navigating to same page with different parameters.
        if (context.IsNavigationIntercepted && _scrollTopModule != null)
        {
            await _scrollTopModule.InvokeVoidAsync("setScrollTop", MainElement, 0);
        }
    }

    private void OnThemeChange(string newTheme)
    {
        Theme = newTheme;
        StateHasChanged();
    }

    public async ValueTask DisposeAsync()
    {
        if (_locationChangingHandler != null)
        {
            _locationChangingHandler?.Dispose();
            _locationChangingHandler = null;
        }

        if (_scrollTopModule != null)
        {
            try
            {
                await _scrollTopModule.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
            }
            _scrollTopModule = null;
        }
    }
}
