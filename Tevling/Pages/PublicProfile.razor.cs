using Microsoft.Extensions.Localization;
using Tevling.Strava;

namespace Tevling.Pages;

public partial class PublicProfile : ComponentBase
{
    private record Medals
    {
        public int First { get; set; }
        public int Second { get; set; }
        public int Third { get; set; }

        public string GetFirst() => First + " 🥇";
        public string GetSecond() => Second + " 🥈";
        public string GetThird() => Third + " 🥉";

        public void Reset()
        {
            First = 0;
            Second = 0;
            Third = 0;
        }
    }

    private record Stats
    {
        public double? LongestRun { get; init; }
        public double? LongestWalk { get; init; }
        public double? LongestRide { get; init; }
        public double? BiggestClimb { get; init; }
        public double? LongestActivity { get; init; }
        public int? NumberOfActivitiesLogged { get; init; }
        public ActivityType? MostPopularActivity { get; init; }
    }

    [Inject] private IAuthenticationService AuthenticationService { get; set; } = null!;
    [Inject] private IStringLocalizer<PublicProfile> Loc { get; set; } = null!;

    [Inject] private IAthleteService AthleteService { get; set; } = null!;

    [Inject] private IBrowserTime BrowserTime { get; set; } = null!;
    [Inject] private IChallengeService ChallengeService { get; set; } = null!;

    [Inject] private IActivityService ActivityService { get; set; } = null!;


    [Parameter] public int AthleteToViewId { get; set; }
    private Athlete Athlete { get; set; } = default!;
    private Athlete? AthleteToView { get; set; }
    private string? CreatedTime;
    private Dictionary<string, (string, string)> ActiveChallenges { get; } = [];
    private Medals AthleteMedals { get; } = new();
    private Stats? AthleteStats { get; set; }


    protected override async Task OnInitializedAsync()
    {
        Athlete = await AuthenticationService.GetCurrentAthleteAsync();
    }

    protected override async Task OnParametersSetAsync()
    {
        Athlete? athleteToView = await AthleteService.GetAthleteByIdAsync(AthleteToViewId);
        if (athleteToView != null)
        {
            AthleteToView = athleteToView;
            DateTimeOffset browserTime = await BrowserTime.ConvertToLocal(AthleteToView!.Created);
            CreatedTime = browserTime.ToString("d");
            ActiveChallenges.Clear();
            AthleteMedals.Reset();
            await FetchActiveChallenges();
            await CountMedals();
            await FetchStats();
        }
    }

    private async Task FetchStats(CancellationToken ct = default)
    {
        ActivityFilter filter = new(
            AthleteToViewId,
            false
        );
        Activity[] activities = await ActivityService.GetActivitiesAsync(filter, ct: ct);
        if (activities.Length == 0)
        {
            AthleteStats = null;
            return;
        }

        List<Activity> runs = [.. activities.Where(a => a.Details.Type is ActivityType.Run)];
        List<Activity> rides = [.. activities.Where(a => a.Details.Type is ActivityType.Ride)];
        List<Activity> walks = [.. activities.Where(a => a.Details.Type is ActivityType.Walk or ActivityType.Hike)];
        Stats athleteStats = new()
        {
            MostPopularActivity = activities.GroupBy(a => a.Details.Type)
                .OrderByDescending(g => g.Count())
                .First()
                .Key,
            LongestRun = runs.Count != 0
                ? runs.Select(a => Math.Round(a.Details.DistanceInMeters / 1000, 1)).Max()
                : null,
            LongestRide = rides.Count != 0
                ? rides.Select(a => Math.Round(a.Details.DistanceInMeters / 1000, 1))
                    .DefaultIfEmpty()
                    .Max()
                : null,
            LongestWalk = walks.Count != 0
                ? walks.Select(a => Math.Round(a.Details.DistanceInMeters / 1000, 1))
                    .DefaultIfEmpty()
                    .Max()
                : null,
            BiggestClimb = activities.Select(a => Math.Round(a.Details.TotalElevationGain)).Max(),
            LongestActivity = activities.Select(a => Math.Round((double)a.Details.MovingTimeInSeconds / 3600)).Max(),
            NumberOfActivitiesLogged = activities.Length,
        };
        AthleteStats = athleteStats;
    }

    private async Task CountMedals(CancellationToken ct = default)
    {
        DateTimeOffset browserTime = await BrowserTime.ConvertToLocal(DateTimeOffset.Now, ct);
        ChallengeFilter filter = new(
            string.Empty,
            null,
            OnlyJoinedChallenges: true,
            IncludeOutdatedChallenges: true);
        Challenge[] challenges = await ChallengeService.GetChallengesAsync(AthleteToView!.Id, filter, null, ct);
        foreach (Challenge challenge in challenges)
        {
            if (challenge.End > browserTime) continue;
            ScoreBoard scoreBoard = await ChallengeService.GetScoreBoardAsync(challenge.Id, ct);

            AthleteScore? score = scoreBoard.Scores.FirstOrDefault(x => x.Name == AthleteToView.Name);
            if (score is null) continue;

            int placement = scoreBoard.Scores.ToList().IndexOf(score) + 1;
            switch (placement)
            {
                case 1:
                    AthleteMedals.First += 1;
                    break;
                case 2:
                    AthleteMedals.Second += 1;
                    break;
                case 3:
                    AthleteMedals.Third += 1;
                    break;
            }
        }
    }

    private async Task FetchActiveChallenges(CancellationToken ct = default)
    {
        DateTimeOffset browserTime = await BrowserTime.ConvertToLocal(DateTimeOffset.Now, ct);
        ChallengeFilter filter = new(
            string.Empty,
            null,
            OnlyJoinedChallenges: true,
            IncludeOutdatedChallenges: false);
        Challenge[] challenges = await ChallengeService.GetChallengesAsync(AthleteToView!.Id, filter, null, ct);

        foreach (Challenge challenge in challenges)
        {
            if (challenge.End <= browserTime) continue;

            ScoreBoard scoreBoard = await ChallengeService.GetScoreBoardAsync(challenge.Id, ct);

            AthleteScore? score = scoreBoard.Scores.FirstOrDefault(x => x.Name == AthleteToView.Name);
            if (score is null) continue;

            int placement = scoreBoard.Scores.ToList().IndexOf(score) + 1;
            if (placement is 0) continue;

            string placementString = GetOrdinal(placement);

            ActiveChallenges[challenge.Title] = (placementString, score.Score);
        }
    }

    private string GetOrdinal(int num)
    {
        if (num <= 0) return num.ToString();

        return num switch
        {
            1 => $"1{Loc["First"]} 🥇",
            2 => $"2{Loc["Second"]} 🥈",
            3 => $"3{Loc["Third"]} 🥉",
            _ => (num % 100) switch
            {
                11 or 12 or 13 => num + Loc["Nth"],
                _ => (num % 10) switch
                {
                    1 => num + Loc["First"],
                    2 => num + Loc["Second"],
                    3 => num + Loc["Third"],
                    _ => num + Loc["Nth"],
                },
            },
        };
    }

    private async Task ToggleFollowing(int followingId)
    {
        Athlete = await AthleteService.ToggleFollowingAsync(Athlete, followingId);

        if (Athlete.Id == AthleteToViewId)
        {
            AthleteToView = await AthleteService.GetAthleteByIdAsync(AthleteToViewId) ??
                throw new InvalidOperationException($"Athlete with ID {AthleteToViewId} not found");
        }

        await InvokeAsync(StateHasChanged);
    }

    private async Task RemoveFollower(int followerId)
    {
        Athlete = await AthleteService.RemoveFollowerAsync(Athlete, followerId);

        if (Athlete.Id == AthleteToViewId)
        {
            AthleteToView = await AthleteService.GetAthleteByIdAsync(AthleteToViewId) ??
                throw new InvalidOperationException($"Athlete with ID {AthleteToViewId} not found");
        }

        await InvokeAsync(StateHasChanged);
    }
}
