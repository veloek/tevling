using Microsoft.FeatureManagement;

namespace Tevling.Components;

public partial class FeatureGate : ComponentBase
{
    [Inject]
    IFeatureManager FeatureManager { get; set; } = null!;

    [Parameter]
    public FeatureFlag? FeatureFlag { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    private bool FeatureEnabled { get; set; }

    protected override async Task OnInitializedAsync()
    {
        if (FeatureFlag != null && await FeatureManager.IsEnabledAsync(FeatureFlag))
        {
            FeatureEnabled = true;
        }
    }
}
