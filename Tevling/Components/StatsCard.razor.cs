
namespace Tevling.Components;

public partial class StatsCard : ComponentBase
{
    [Parameter] public string Stat { get; set; } = "";
    [Parameter] public string Value { get; set; } = "";
    [Parameter] public string AdditionalValueClass { get; set; } = "";
}

