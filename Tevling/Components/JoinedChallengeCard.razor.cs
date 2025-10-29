
namespace Tevling.Components;

public partial class JoinedChallengeCard : ComponentBase
{
    [Parameter] public string Title { get; set; } = "";
    [Parameter] public string Placement { get; set; } = "";
    [Parameter] public string Score { get; set; } = "";
}

