using Tevling.Shared;
using Tevling.Strava;
using Athlete = Tevling.Model.Athlete;

namespace Tevling.Components;

public partial class ChallengeForm : ComponentBase
{
    [Inject] private IAuthenticationService AuthenticationService { get; set; } = null!;
    [Inject] private IAthleteService AthleteService { get; set; } = null!;
    [Inject] private IChallengeService ChallengeService { get; set; } = null!;

    [Parameter] public string SubmitLabel { get; set; } = "Submit";
    [Parameter] public string CancelLabel { get; set; } = "Cancel";
    [Parameter] public Action<ChallengeFormModel>? OnSubmit { get; set; }
    [Parameter] public Func<ChallengeFormModel, Task>? OnSubmitAsync { get; set; }
    [Parameter] public Action? OnCancel { get; set; }
    [Parameter] public Func<Task>? OnCancelAsync { get; set; }
    [Parameter] public Challenge? EditChallenge { get; set; }

    [SupplyParameterFromForm] public ChallengeFormModel Challenge { get; set; } = new();

    private List<ChallengeTemplate> Templates { get; set; } = [];
    private Dictionary<int, bool> TemplatesSelectedForDeletion { get; set; } = [];
    private const int MaximumSuggestions = 10;
    private DropdownSearch<ActivityType>? _dropdownSearchRefActivityTypes;
    private DropdownSearch<Athlete>? _dropdownSearchRefAthletes;
    private Athlete Athlete { get; set; } = default!;
    private static IEnumerable<ActivityType> ActivityTypes => Enum.GetValues(typeof(ActivityType)).Cast<ActivityType>();
    private static Func<Athlete, string> AthletesDisplayFunc => athlete => athlete.Name;

    protected override async Task OnInitializedAsync()
    {
        Athlete = await AuthenticationService.GetCurrentAthleteAsync();
        ChallengeTemplate[] templates = await ChallengeService.GetChallengeTemplatesAsync(Athlete.Id);
        Templates = [.. templates];
        foreach (ChallengeTemplate challengeTemplate in templates)
        {
            TemplatesSelectedForDeletion[challengeTemplate.Id] = false;
        }
    }

    private void DeleteTemplates()
    {
        int[] keysToDelete = [.. TemplatesSelectedForDeletion.Where(t => t.Value).Select(t => t.Key)];
        foreach (int key in keysToDelete)
        {
            _ = DeleteTemplate(key);
            TemplatesSelectedForDeletion.Remove(key);
        }
    }

    private async Task DeleteTemplate(int templateId)
    {
        await ChallengeService.DeleteChallengeTemplateAsync(templateId);
        Templates = [.. await ChallengeService.GetChallengeTemplatesAsync(Athlete.Id)];
    }

    private void LoadSelectedTemplate(object? value)
    {
        if (value is not null && int.TryParse(value.ToString(), out int index) && index < Templates.Count)
        {
            LoadTemplate(Templates[index]);
        }
    }

    private void LoadTemplate(ChallengeTemplate template)
    {
        Challenge = new ChallengeFormModel
        {
            Start = DateTimeOffset.Now,
            End = DateTimeOffset.Now.AddMonths(1),
            CreatedBy = Athlete.Id,
            Title = template.Title,
            Description = template.Description,
            Measurement = template.Measurement,
            ActivityTypes = template.ActivityTypes,
            IsPrivate = template.IsPrivate,
            InvitedAthletes = [],
        };
    }

    private async Task CreateChallengeTemplate()
    {
        ChallengeTemplate newChallengeTemplate = new()
        {
            Title = Challenge.Title,
            Description = Challenge.Description,
            Measurement = Challenge.Measurement,
            ActivityTypes = [.. Challenge.ActivityTypes],
            IsPrivate = Challenge.IsPrivate,
            Created = DateTimeOffset.Now,
            CreatedById = Challenge.CreatedBy,
        };
        await ChallengeService.CreateChallengeTemplateAsync(newChallengeTemplate);
        Templates = [.. await ChallengeService.GetChallengeTemplatesAsync(Athlete.Id)];

        foreach (ChallengeTemplate challengeTemplate in Templates)
        {
            TemplatesSelectedForDeletion[challengeTemplate.Id] = false;
        }
    }
    
    protected override void OnParametersSet()
    {
        if (EditChallenge != null)
        {
            if (EditChallenge.Athletes is null) throw new Exception("Athletes not initialized");

            Challenge.Title = EditChallenge.Title;
            Challenge.Description = EditChallenge.Description;
            Challenge.Start = EditChallenge.Start;
            Challenge.End = EditChallenge.End;
            Challenge.Measurement = EditChallenge.Measurement;
            Challenge.ActivityTypes = EditChallenge.ActivityTypes.ToList();
            Challenge.IsPrivate = EditChallenge.IsPrivate;
            Challenge.CreatedBy = EditChallenge.CreatedById;
            Challenge.InvitedAthletes = EditChallenge.InvitedAthletes?.ToList() ?? []
                ;
        }
        else
        {
            Challenge = new ChallengeFormModel
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
            NotIn = Challenge.InvitedAthletes.Select(a => a.Id).Append(Athlete.Id),
        };

        Athlete[] result = await AthleteService.GetAthletesAsync(filter, new Paging(MaximumSuggestions));

        return result;
    }

    private async Task DeselectActivityType(ActivityType item)
    {
        if (_dropdownSearchRefActivityTypes is null) return;

        await _dropdownSearchRefActivityTypes.DeselectItemAsync(item);
    }


    private async Task DeselectAthlete(Athlete item)
    {
        if (_dropdownSearchRefAthletes is null) return;

        await _dropdownSearchRefAthletes.DeselectItemAsync(item);
    }

    private async Task SubmitForm()
    {
        OnSubmit?.Invoke(Challenge);

        if (OnSubmitAsync != null) await OnSubmitAsync(Challenge);
    }

    private async Task Cancel()
    {
        OnCancel?.Invoke();

        if (OnCancelAsync != null) await OnCancelAsync();
    }
}
