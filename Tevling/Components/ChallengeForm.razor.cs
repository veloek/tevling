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
            Challenge.ActivityTypes = EditChallenge.ActivityTypes;
            Challenge.IsPrivate = EditChallenge.IsPrivate;
            Challenge.CreatedBy = EditChallenge.CreatedById;
            Challenge.InvitedAthletes = EditChallenge.InvitedAthletes?.ToList();
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
        AthleteFilter filter = new() { SearchText = searchText, NotIn = Challenge.InvitedAthletes?.Select(a => a.Id) };
        Athlete[] result = await AthleteService.GetAthletesAsync(filter, MaximumSuggestions, 0);

        return result;
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
