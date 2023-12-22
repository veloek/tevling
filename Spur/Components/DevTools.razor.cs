namespace Spur.Components;

public partial class DevTools : ComponentBase
{
    [Inject]
    IActivityService ActivityService { get; set; } = null!;

    [Inject]
    IAthleteService AthleteService { get; set; } = null!;

    [Inject]
    IChallengeService ChallengeService { get; set; } = null!;

    [Parameter]
    public Athlete? Athlete { get; set; }

    private bool NoOtherAthletes =>
        _athletes.Length == 1;

    private Athlete[] _athletes = [];

    protected override async Task OnInitializedAsync()
    {
        _athletes = await AthleteService.GetAthletesAsync(null, 100);
    }

    private Task AddActivity()
    {
        if (Athlete is null)
            throw new ArgumentException(nameof(Athlete));

        return ActivityService.CreateActivityAsync(Athlete.StravaId, Random.Shared.Next(1000, 10000));
    }

    private Task AddOthersActivity()
    {

        long randomAthleteStravaId;
        do
        {
            randomAthleteStravaId = _athletes[Random.Shared.Next(0, _athletes.Length)].StravaId;
        } while (randomAthleteStravaId == Athlete?.StravaId);

        return ActivityService.CreateActivityAsync(randomAthleteStravaId, Random.Shared.Next(1000, 10000));
    }

    private async Task AddAthlete()
    {
        int id = Random.Shared.Next(100, 1000);
        await AthleteService.UpsertAthleteAsync(Random.Shared.Next(10000, 100000), $"Athlete {id}", null, "", "", default);
        _athletes = await AthleteService.GetAthletesAsync(null, 100);
    }

    private Task AddOthersChallenge()
    {

        int randomAthleteId;
        do
        {
            randomAthleteId = _athletes[Random.Shared.Next(0, _athletes.Length)].Id;
        } while (randomAthleteId == Athlete?.Id);

        return ChallengeService.CreateChallengeAsync(new ChallengeFormModel
        {
            Title = $"Challenge {Random.Shared.Next(1000, 10000)}",
            Description = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.",
            Start = DateTimeOffset.Now,
            End = DateTimeOffset.Now.AddMonths(1),
            Measurement = ChallengeMeasurement.Distance,
            ActivityTypes = [Strava.ActivityType.Run],
            CreatedBy = randomAthleteId,
        });
    }
}
