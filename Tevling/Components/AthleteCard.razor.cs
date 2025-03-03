namespace Tevling.Components;

public partial class AthleteCard : ComponentBase
{
    [Parameter] public Athlete? Athlete { get; set; }
    [Parameter] public bool IsFollowing { get; set; }
    [Parameter] public bool IsFollower { get; set; }
    [Parameter] public EventCallback FollowingStatusChanged { get; set; }
    [Parameter] public EventCallback RemoveFollower { get; set; }
}
