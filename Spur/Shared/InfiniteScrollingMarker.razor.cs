using Microsoft.JSInterop;

namespace Spur.Shared;

public partial class InfiniteScrollingMarker : ComponentBase, IAsyncDisposable
{
    [Inject]
    IJSRuntime JSRuntime { get; set; } = null!;

    private CancellationTokenSource _cts = new();
    private ElementReference _markerRef;
    private ElementReference MarkerRef {
        get => _markerRef;
        set
        {
            _markerRef = value;
            OnRefChange();
        }
    }
    private bool _isLoading;
    private DotNetObjectReference<InfiniteScrollingMarker>? _objectReference;
    private IJSObjectReference? _module;
    private IJSObjectReference? _instance;
    private int _initialized = 0; // cannot use bool because of Interlocked.Exchange

    [Parameter]
    public Func<CancellationToken, Task>? LoadMore { get; set; }

    [Parameter]
    public bool HasMore { get; set; }

    [Parameter]
    public RenderFragment? LoadingTemplate { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _module = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./infinite-scrolling-marker.js");
            _objectReference = DotNetObjectReference.Create(this);
        }

        // Multiple OnAfterRenderAsync calls may happen concurrently,
        // so we have to make sure we only initialize once.
        if (0 == Interlocked.Exchange(ref _initialized, 1))
        {
            if (_instance != null)
            {
                await _instance.InvokeVoidAsync("dispose");
                await _instance.DisposeAsync();
            }
            _instance = await _module!.InvokeAsync<IJSObjectReference>("initialize", _markerRef, _objectReference);
        }
    }

    private void OnRefChange()
    {
        // Re-initialize when the marker ref changes
        _initialized = 0;
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
                await LoadMore.Invoke(_cts.Token);
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
