namespace Tevling.Shared;

public partial class DropdownSearch<T> : ComponentBase
{
    [Parameter] public IEnumerable<T> Items { get; set; } = [];
    [Parameter] public Func<T, string>? DisplayFunc { get; set; }
    [Parameter] public Func<string, Task<IEnumerable<T>>>? CustomSearchFuncAsync { get; set; }
    [Parameter] public EventCallback<ICollection<T>> SelectedItemsChanged { get; set; }
    [Parameter] public ICollection<T> SelectedItems { get; set; } = [];

    private Timer? DebounceTimer;
    private bool IsSearchFocused = false;
    private Timer? SearchInputBlurTimer;
    private string SearchTerm = string.Empty;
    private IEnumerable<T> FilteredItems { get; set; } = [];

    protected override async Task OnInitializedAsync()
    {
        if (CustomSearchFuncAsync is null)
            await FilterItems();
    }

    private void OnSearchInput(ChangeEventArgs e)
    {
        SearchTerm = e.Value?.ToString() ?? string.Empty;

        DebounceTimer?.Dispose();
        DebounceTimer = new Timer(
            async _ => { await InvokeAsync(async () => { await FilterItems(); }); },
            null,
            500,
            Timeout.Infinite);
    }

    private async Task FilterItems()
    {
        if (CustomSearchFuncAsync is not null)
        {
            if (SearchTerm == string.Empty)
            {
                DebounceTimer?.Dispose();
                FilteredItems = [];
            }
            else
            {
                FilteredItems = await CustomSearchFuncAsync(SearchTerm);
            }
        }
        else
        {
            FilteredItems = Items.Where(
                item => DisplayFunc != null &&
                    DisplayFunc(item).Contains(SearchTerm, StringComparison.OrdinalIgnoreCase));
        }

        StateHasChanged();
    }


    private void OnInputBlur()
    {
        SearchInputBlurTimer?.Dispose();
        SearchInputBlurTimer = new Timer(
            _ =>
            {
                InvokeAsync(
                    async () =>
                    {
                        IsSearchFocused = false;
                        SearchTerm = string.Empty;
                        StateHasChanged();
                        await FilterItems();
                    });
            },
            null,
            200,
            Timeout.Infinite);
    }

    private async Task SelectItemAsync(T item)
    {
        List<T> newSelection = [..SelectedItems, item];
        SelectedItems = newSelection;
        await SelectedItemsChanged.InvokeAsync(newSelection);
    }


    public async Task DeselectItemAsync(T item)
    {
        if (!SelectedItems.Contains(item))
            return;

        List<T> newSelection = SelectedItems.Where(se => se is not null && !se.Equals(item)).ToList();
        SelectedItems = newSelection;
        await SelectedItemsChanged.InvokeAsync(newSelection);
    }

    public void Dispose()
    {
        DebounceTimer?.Dispose();
        SearchInputBlurTimer?.Dispose();
    }
}
