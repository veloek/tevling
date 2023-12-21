using Microsoft.JSInterop;

namespace Spur.Shared;

public partial class InfiniteScrollingMarker : ComponentBase, IAsyncDisposable
{
    [Inject]
    IJSRuntime JSRuntime { get; set; } = null!;

    private CancellationTokenSource _cts = new();
    private ElementReference _markerRef;
    private bool _isLoading;
    private bool _hasMore = true;
    private DotNetObjectReference<InfiniteScrollingMarker>? _objectReference;
    private IJSObjectReference? _module;
    private IJSObjectReference? _instance;

    [Parameter]
    public Func<CancellationToken, Task<bool>>? LoadMore { get; set; }

    [Parameter]
    public RenderFragment? LoadingTemplate { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
            return;

        _module = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./infinite-scrolling-marker.js");
        _objectReference = DotNetObjectReference.Create(this);
        _instance = await _module.InvokeAsync<IJSObjectReference>("initialize", _markerRef, _objectReference);
    }

    [JSInvokable]
    public async Task OnMarkerVisible()
    {
        if (_isLoading)
        {
            return;
        }

        if (LoadMore != null)
        {
            _isLoading = true;
            StateHasChanged();

            try
            {
                _hasMore = await LoadMore.Invoke(_cts.Token);
            }
            catch (OperationCanceledException e) when (e.CancellationToken == _cts.Token)
            {
                // Ignore cancellations
            }
            finally
            {
                _isLoading = false;
                StateHasChanged();
            }

            if (_instance != null)
            {
                await _instance.InvokeVoidAsync("onNewItems");
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            _cts.Dispose();
            if (_instance != null)
            {
                await _instance.InvokeVoidAsync("dispose");
                await _instance.DisposeAsync();

                _instance = null;
            }

            if (_module != null)
            {
                await _module.DisposeAsync();
                _module = null;
            }

            _objectReference?.Dispose();
        }
        catch (JSDisconnectedException)
        {
            // Ignore, happens during page reload.
        }
    }
}
