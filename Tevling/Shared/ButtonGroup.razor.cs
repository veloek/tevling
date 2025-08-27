using Microsoft.AspNetCore.Components;

namespace Tevling.Shared;

public partial class ButtonGroup<T> : ComponentBase
{
    [Parameter] public IEnumerable<T> Items { get; set; } = [];
    [Parameter] public T? SelectedItem { get; set; }
    
    [Parameter] public EventCallback<T> SelectedItemChanged { get; set; }
    [Parameter] public EventCallback<T> OnButtonSelected { get; set; }
    
    protected override void OnInitialized()
    {
        if (SelectedItem is null && Items.Any())
        {
            SelectedItem = Items.First();
        }
    }

    private string GetButtonCssClass(T item)
    {
        return SelectedItem?.Equals(item) == true ? "btn btn-primary active me-2" : "btn btn-secondary me-2";
    }

    private async Task HandleButtonClick(T item)
    {
        SelectedItem = item;
        if(SelectedItemChanged.HasDelegate)
        {
            await SelectedItemChanged.InvokeAsync(item);
            await OnButtonSelected.InvokeAsync(item);
        }
    }
}

