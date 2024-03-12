using Tevling.Strava;
using Athlete = Tevling.Model.Athlete;
using Tevling.Shared;

namespace Tevling.Components;

public partial class ChallengeForm : ComponentBase
{
    [Inject]
    IAuthenticationService AuthenticationService { get; set; } = null!;

    [Inject]
    IAthleteService AthleteService { get; set; } = null!;

    [Parameter]
    public string SubmitLabel { get; set; } = "Submit";

    [Parameter]
    public string CancelLabel { get; set; } = "Cancel";

    [Parameter]
    public Action<ChallengeFormModel>? OnSubmit { get; set; }

    [Parameter]
    public Func<ChallengeFormModel, Task>? OnSubmitAsync { get; set; }

    [Parameter]
    public Action? OnCancel { get; set; }

    [Parameter]
    public Func<Task>? OnCancelAsync { get; set; }

    [Parameter]
    public Challenge? EditChallenge { get; set; }

    [SupplyParameterFromForm]
    public ChallengeFormModel Challenge { get; set; } = new();

    private Athlete Athlete { get; set; } = default!;

    private const int MaximumSuggestions = 10;

    private DropdownSearch<ActivityType>? dropdownSearchRefActivityTypes;
    private DropdownSearch<Athlete>? dropdownSearchRefAthletes;

    private static IEnumerable<ActivityType> ActivityTypes => Enum.GetValues(typeof(ActivityType)).Cast<ActivityType>();
    private static Func<ActivityType, string> ActivityTypeDisplayFunc => 
        activityType => string.Concat(activityType.ToString().Select((x, i) => i > 0 && char.IsUpper(x) ? " " + x : x.ToString()));
    
    private static Func<Athlete, string> AthletesDisplayFunc => athlete => athlete.Name;

    protected override async Task OnInitializedAsync()
    {
        Athlete = await AuthenticationService.GetCurrentAthleteAsync();
    }

    protected override void OnParametersSet()
    {
        if (EditChallenge != null)
        {
            if (EditChallenge.Athletes is null)
            {
                throw new Exception("Athletes not initialized");
            }
            Challenge.Title = EditChallenge.Title;
            Challenge.Description = EditChallenge.Description;
            Challenge.Start = EditChallenge.Start;
            Challenge.End = EditChallenge.End;
            Challenge.Measurement = EditChallenge.Measurement;
            Challenge.ActivityTypes = EditChallenge.ActivityTypes.ToList();
            Challenge.IsPrivate = EditChallenge.IsPrivate;
            Challenge.CreatedBy = EditChallenge.CreatedById;
            Challenge.InvitedAthletes =EditChallenge.InvitedAthletes?.ToList() ?? [];
        }
        else
        {
            Challenge = new()
            {
                Start = DateTimeOffset.Now,
                End = DateTimeOffset.Now.AddMonths(1),
                CreatedBy = Athlete.Id,
            };
        }
    }

    private async Task<IEnumerable<Athlete>> SearchAthletes(string searchText)
    {
        AthleteFilter filter = new()
        {
            SearchText = searchText,
            NotIn = Challenge.InvitedAthletes?.Select(a => a.Id).Append(Athlete.Id)
        };
        Athlete[] result = await AthleteService.GetAthletesAsync(filter, new Paging(MaximumSuggestions));

        return result;
    }

    private async Task DeselectActivityType(ActivityType item)
    {
        if (dropdownSearchRefActivityTypes is null) return;
        Challenge.ActivityTypes.Remove(item);

        await dropdownSearchRefActivityTypes.DeselectItemAsync(item);
    }


    private async Task DeselectAthlete(Athlete item)
    {
        if (dropdownSearchRefAthletes is null) return;
        Challenge.InvitedAthletes?.Remove(item);

        await dropdownSearchRefAthletes.DeselectItemAsync(item);
    }

    private async Task SubmitForm()
    {
        OnSubmit?.Invoke(Challenge);

        if (OnSubmitAsync != null)
        {
            await OnSubmitAsync(Challenge);
        }
    }

    private async Task Cancel()
    {
        OnCancel?.Invoke();

        if (OnCancelAsync != null)
        {
            await OnCancelAsync();
        }
    }
}
