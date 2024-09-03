namespace Tevling.Components;

public partial class DevTools : ComponentBase
{
    [Inject] private IActivityService ActivityService { get; set; } = null!;
    [Inject] private IAthleteService AthleteService { get; set; } = null!;
    [Inject] private IChallengeService ChallengeService { get; set; } = null!;

    [Parameter] public Athlete? Athlete { get; set; }

    private Athlete[] _athletes = [];
    private bool NoOtherAthletes => _athletes.Length == 1;
    private int ClearChallengeWinnerId { get; set; }

    protected override async Task OnInitializedAsync()
    {
        _athletes = await AthleteService.GetAthletesAsync();
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
        await AthleteService.UpsertAthleteAsync(Random.Shared.Next(10000, 100000), $"Athlete {id}", null, "", "",
            default);
        _athletes = await AthleteService.GetAthletesAsync();
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
            Description =
                "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.",
            Start = DateTimeOffset.Now,
            End = DateTimeOffset.Now.AddMonths(1),
            Measurement = ChallengeMeasurement.Distance,
            ActivityTypes = [Strava.ActivityType.Run],
            CreatedBy = randomAthleteId
        });
    }

    private Task AddOthersPrivateChallenge()
    {
        int randomAthleteId;
        do
        {
            randomAthleteId = _athletes[Random.Shared.Next(0, _athletes.Length)].Id;
        } while (randomAthleteId == Athlete?.Id);

        return ChallengeService.CreateChallengeAsync(new ChallengeFormModel
        {
            Title = $"Private Challenge {Random.Shared.Next(1000, 10000)}",
            Description =
                "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.",
            Start = DateTimeOffset.Now,
            End = DateTimeOffset.Now.AddMonths(1),
            Measurement = ChallengeMeasurement.Distance,
            ActivityTypes =  [Strava.ActivityType.Run],
            IsPrivate = true,
            CreatedBy = randomAthleteId
        });
    }

    private Task AddOthersPrivateChallengeInvited()
    {
        int randomAthleteId;
        do
        {
            randomAthleteId = _athletes[Random.Shared.Next(0, _athletes.Length)].Id;
        } while (randomAthleteId == Athlete?.Id);

        if (Athlete is null)
            throw new ArgumentException(nameof(Athlete));

        return ChallengeService.CreateChallengeAsync(new ChallengeFormModel
        {
            Title = $"Private Challenge {Random.Shared.Next(1000, 10000)}",
            Description =
                "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.",
            Start = DateTimeOffset.Now,
            End = DateTimeOffset.Now.AddMonths(1),
            Measurement = ChallengeMeasurement.Distance,
            ActivityTypes = [Strava.ActivityType.Run],
            IsPrivate = true,
            CreatedBy = randomAthleteId,
            InvitedAthletes = [new Athlete() { Id = Athlete.Id }],
        });
    }

    private async Task ImportAllAthletesActivities(int nDays)
    {
        DateTimeOffset from = DateTimeOffset.Now - TimeSpan.FromDays(nDays);
        Athlete[] athletes = await AthleteService.GetAthletesAsync();
        foreach (Athlete athlete in athletes) await ActivityService.ImportActivitiesForAthlete(athlete.Id, from);
    }

    private async Task AddChallenge()
    {
        if (Athlete is null)
            throw new ArgumentException(nameof(Athlete));

        await ChallengeService.CreateChallengeAsync(new ChallengeFormModel
        {
            Title = $"Challenge {Random.Shared.Next(1000, 10000)}",
            Description =
                "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.",
            Start = DateTimeOffset.Now,
            End = DateTimeOffset.Now.AddMonths(1),
            Measurement = ChallengeMeasurement.Distance,
            ActivityTypes = [Strava.ActivityType.Run],
            IsPrivate = false,
            CreatedBy = Athlete.Id
        });
    }

    private async Task ClearChallengeWinner()
    {
        if (ClearChallengeWinnerId > 0)
        {
            await ChallengeService.ClearChallengeWinnerAsync(ClearChallengeWinnerId);
        }
    }
}
