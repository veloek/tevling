using System.Globalization;
using System.Reactive.Subjects;
using Microsoft.EntityFrameworkCore;

namespace Tevling.Services;

public class ChallengeService(
    ILogger<ChallengeService> logger,
    IDbContextFactory<DataContext> dataContextFactory)
    : IChallengeService
{
    private readonly Subject<FeedUpdate<Challenge>> _challengeFeed = new();

    public async Task<Challenge?> GetChallengeByIdAsync(int challengeId, CancellationToken ct = default)
    {
        await using DataContext dataContext = await dataContextFactory.CreateDbContextAsync(ct);

        Challenge? challenge = await dataContext.Challenges
            .Include(c => c.Athletes)
            .Include(c => c.InvitedAthletes)
            .Include(c => c.CreatedBy)
            .AsSplitQuery()
            .FirstOrDefaultAsync(c => c.Id == challengeId, ct);

        return challenge;
    }

    public async Task<Challenge[]> GetChallengesAsync(
        int currentAthleteId,
        ChallengeFilter filter,
        Paging? paging = null,
        CancellationToken ct = default)
    {
        await using DataContext dataContext = await dataContextFactory.CreateDbContextAsync(ct);

        Challenge[] challenges = await dataContext.Challenges
            .Include(challenge => challenge.Athletes)
            .Include(challenge => challenge.Winner)
            .Include(challenge => challenge.InvitedAthletes)
            .Include(challenge => challenge.CreatedBy)
            .AsSplitQuery()
            .Where(
                challenge => filter.OnlyJoinedChallenges
                    ? challenge.Athletes!.Any(a => a.Id == currentAthleteId)
                    : !challenge.IsPrivate ||
                    challenge.InvitedAthletes!.Any(a => a.Id == currentAthleteId) ||
                    challenge.CreatedById == currentAthleteId)
            .Where(
                challenge => !filter.ByAthleteId.HasValue ||
                    challenge.Athletes!.Any(athlete => athlete.Id == filter.ByAthleteId.Value) == true ||
                    challenge.CreatedById == filter.ByAthleteId.Value)
            // TODO: Filter out outdated challenges when DB supports it
            //       (Time to switch to PostgreSQL?)
            // .Where(challenge => filter.IncludeOutdatedChallenges
            //     || challenge.End.UtcDateTime.Date >= DateTimeOffset.UtcNow.Date)
            .Where(
                c => string.IsNullOrWhiteSpace(filter.SearchText) ||
                    // TODO: Use EF.Functions.ILike when switching to PostgreSQL
                    //       to keep the search text case-insensitive
                    EF.Functions.Like(c.Title, $"%{filter.SearchText}%"))
            .Where(c =>
                (filter.IncludeTimeChallenges && c.Measurement == ChallengeMeasurement.Time) ||
                (filter.IncludeElevationChallenges && c.Measurement == ChallengeMeasurement.Elevation) ||
                (filter.IncludeDistanceChallenges && c.Measurement == ChallengeMeasurement.Distance))
            .Where(c => filter.ActivityTypes == null || filter.ActivityTypes.Count <= 0 ||
                filter.ActivityTypes.Intersect(c.ActivityTypes).Any())
            .OrderByDescending(challenge => challenge.Start)
            .ThenBy(challenge => challenge.Title)
            .ThenBy(challenge => challenge.Id)
            .If(paging != null, x => x.Skip(paging!.Value.Page * paging!.Value.PageSize), x => x)
            .If(paging != null, x => x.Take(paging!.Value.PageSize))
            .ToArrayAsync(ct);

        // TODO: Filter this in query when DB supports it
        if (!filter.IncludeOutdatedChallenges)
        {
            Challenge[] activeChallenges = [.. challenges.Where(c => c.End.UtcDateTime.Date >= DateTimeOffset.UtcNow.Date)];
            return activeChallenges;
        }

        return challenges;
    }

    public async Task<ChallengeGroup> CreateChallengeGroupAsync(ChallengeGroup newChallengeGroup, CancellationToken ct = default)
    {
        await using DataContext dataContext = await dataContextFactory.CreateDbContextAsync(ct);

        logger.LogInformation("Adding new challenge group: {Name}", newChallengeGroup.Name);

        foreach (Athlete member in newChallengeGroup.Members ?? [])
        {
            dataContext.Attach(member);
        }

        ChallengeGroup challengeGroup = await dataContext.AddChallengeGroupAsync(
            newChallengeGroup,
            ct);

        return challengeGroup;
    }

    public async Task<ChallengeGroup[]> GetChallengeGroupsAsync(int currentAthleteId, CancellationToken ct = default)
    {
        await using DataContext dataContext = await dataContextFactory.CreateDbContextAsync(ct);

        ChallengeGroup[] groups = await dataContext.ChallengeGroups
            .Include(group => group.Members)
            .Where(group => group.CreatedById == currentAthleteId)
            .ToArrayAsync(ct);

        return groups;
    }

    public async Task DeleteChallengeGroupAsync(int challengeGroupId, CancellationToken ct = default)
    {
        await using DataContext dataContext = await dataContextFactory.CreateDbContextAsync(ct);

        logger.LogInformation("Deleting challenge group: {id}", challengeGroupId);

        ChallengeGroup challengeGroup = await dataContext.ChallengeGroups
                .FirstOrDefaultAsync(g => g.Id == challengeGroupId, ct) ??
            throw new Exception($"Unknown challenge group ID {challengeGroupId}");

        _ = await dataContext.RemoveChallengeGroupAsync(challengeGroup, ct);
    }

    public async Task<ChallengeTemplate[]> GetChallengeTemplatesAsync(
        int currentAthleteId,
        CancellationToken ct = default)
    {
        await using DataContext dataContext = await dataContextFactory.CreateDbContextAsync(ct);

        ChallengeTemplate[] challengeTemplates = await dataContext.ChallengeTemplates
            .Include(challengeTemplate => challengeTemplate.CreatedBy)
            .Where(challengeTemplate => challengeTemplate.CreatedById == currentAthleteId)
            .ToArrayAsync(ct);

        return challengeTemplates;
    }

    public IObservable<FeedUpdate<Challenge>> GetChallengeFeed(int athleteId)
    {
        return _challengeFeed.AsObservable()
            .Where(update =>
                !update.Item.IsPrivate ||
                (update.Item.InvitedAthletes?.Any(a => a.Id == athleteId) ?? false) ||
                update.Item.CreatedById == athleteId);
    }

    public async Task<Challenge> CreateChallengeAsync(ChallengeFormModel newChallenge, CancellationToken ct = default)
    {
        await using DataContext dataContext = await dataContextFactory.CreateDbContextAsync(ct);

        logger.LogInformation("Adding new challenge: {Title}", newChallenge.Title);

        foreach (Athlete athlete in newChallenge.InvitedAthletes)
            await dataContext.Entry(athlete).ReloadAsync(ct);

        Challenge challenge = await dataContext.AddChallengeAsync(
            new Challenge
            {
                Title = newChallenge.Title,
                Description = newChallenge.Description,
                Start = newChallenge.Start,
                End = newChallenge.End,
                Measurement = newChallenge.Measurement,
                ActivityTypes = newChallenge.ActivityTypes.ToList(),
                IsPrivate = newChallenge.IsPrivate,
                Created = DateTimeOffset.Now,
                CreatedById = newChallenge.CreatedBy,
                InvitedAthletes = newChallenge.InvitedAthletes,
            },
            ct);

        await dataContext.Entry(challenge).Collection(c => c.Athletes!).LoadAsync(ct);

        _challengeFeed.OnNext(new FeedUpdate<Challenge> { Item = challenge, Action = FeedAction.Create });

        return challenge;
    }

    public async Task<ChallengeTemplate> CreateChallengeTemplateAsync(ChallengeTemplate newChallengeTemplate,
        CancellationToken ct = default)
    {
        await using DataContext dataContext = await dataContextFactory.CreateDbContextAsync(ct);

        logger.LogInformation("Adding new challenge template: {Title}", newChallengeTemplate.Title);

        ChallengeTemplate challengeTemplate = await dataContext.AddChallengeTemplateAsync(
            newChallengeTemplate,
            ct);

        return challengeTemplate;
    }

    public async Task<Challenge> UpdateChallengeAsync(
        int challengeId,
        ChallengeFormModel editChallenge,
        CancellationToken ct = default)
    {
        await using DataContext dataContext = await dataContextFactory.CreateDbContextAsync(ct);

        Challenge challenge = await dataContext.Challenges
                .Include(c => c.Athletes)
                .Include(c => c.CreatedBy)
                .Include(c => c.InvitedAthletes)
                .AsSplitQuery()
                .AsTracking()
                .FirstOrDefaultAsync(c => c.Id == challengeId, ct) ??
            throw new Exception($"Unknown challenge ID {challengeId}");

        logger.LogInformation("Updating challenge ID {ChallengeId}", challengeId);


        bool StillInvited(Athlete a)
        {
            return editChallenge.InvitedAthletes.Any(i => i.Id == a.Id);
        }

        IEnumerable<Athlete> newInvites = editChallenge.InvitedAthletes
            .Where(i => !challenge.InvitedAthletes!.Any(ii => ii.Id == i.Id));

        challenge.InvitedAthletes = challenge.InvitedAthletes!
            .Where(StillInvited)
            .Concat(newInvites)
            .ToList();

        challenge.Title = editChallenge.Title;
        challenge.Description = editChallenge.Description;
        challenge.Start = editChallenge.Start;
        challenge.End = editChallenge.End;
        challenge.Measurement = editChallenge.Measurement;
        challenge.ActivityTypes = editChallenge.ActivityTypes.ToList();
        challenge.IsPrivate = editChallenge.IsPrivate;

        challenge = await dataContext.UpdateChallengeAsync(challenge, CancellationToken.None);

        _challengeFeed.OnNext(new FeedUpdate<Challenge> { Item = challenge, Action = FeedAction.Update });

        return challenge;
    }

    public async Task<Challenge> JoinChallengeAsync(int athleteId, int challengeId, CancellationToken ct = default)
    {
        await using DataContext dataContext = await dataContextFactory.CreateDbContextAsync(ct);

        Challenge challenge = await dataContext.Challenges
                .Include(c => c.Athletes)
                .Include(c => c.CreatedBy)
                .AsTracking()
                .FirstOrDefaultAsync(c => c.Id == challengeId, ct) ??
            throw new Exception($"Challenge ID {challengeId} not found");

        Athlete athlete = await dataContext.Athletes
                .AsTracking()
                .FirstOrDefaultAsync(a => a.Id == athleteId, ct) ??
            throw new Exception($"Athlete ID {athleteId} not found");

        challenge.Athletes!.Add(athlete);

        await dataContext.UpdateChallengeAsync(challenge, ct);

        _challengeFeed.OnNext(new FeedUpdate<Challenge> { Item = challenge, Action = FeedAction.Update });

        return challenge;
    }

    public async Task<Challenge> LeaveChallengeAsync(int athleteId, int challengeId, CancellationToken ct = default)
    {
        await using DataContext dataContext = await dataContextFactory.CreateDbContextAsync(ct);

        Challenge challenge = await dataContext.Challenges
                .Include(c => c.Athletes)
                .Include(c => c.CreatedBy)
                .AsTracking()
                .FirstOrDefaultAsync(c => c.Id == challengeId, ct) ??
            throw new Exception($"Challenge ID {challengeId} not found");

        Athlete? athlete = challenge.Athletes!.FirstOrDefault(a => a.Id == athleteId);

        if (athlete != null) challenge.Athletes!.Remove(athlete);

        await dataContext.UpdateChallengeAsync(challenge, ct);

        _challengeFeed.OnNext(new FeedUpdate<Challenge> { Item = challenge, Action = FeedAction.Update });

        return challenge;
    }

    public async Task DeleteChallengeAsync(int challengeId, CancellationToken ct = default)
    {
        await using DataContext dataContext = await dataContextFactory.CreateDbContextAsync(ct);

        Challenge challenge = await dataContext.Challenges
                .FirstOrDefaultAsync(c => c.Id == challengeId, ct) ??
            throw new Exception($"Unknown challenge ID {challengeId}");

        logger.LogInformation("Deleting challenge ID {ChallengeId}", challengeId);
        _ = await dataContext.RemoveChallengeAsync(challenge, ct);

        _challengeFeed.OnNext(new FeedUpdate<Challenge> { Item = challenge, Action = FeedAction.Delete });
    }

    public async Task DeleteChallengeTemplateAsync(int templateId, CancellationToken ct = default)
    {
        await using DataContext dataContext = await dataContextFactory.CreateDbContextAsync(ct);

        logger.LogInformation("Deleting challenge template: {id}", templateId);

        ChallengeTemplate challengeTemplate = await dataContext.ChallengeTemplates
                .FirstOrDefaultAsync(c => c.Id == templateId, ct) ??
            throw new Exception($"Unknown challenge template ID {templateId}");

        _ = await dataContext.RemoveChallengeTemplateAsync(challengeTemplate, ct);
    }

    public async Task<ScoreBoard> GetScoreBoardAsync(int challengeId, CancellationToken ct = default)
    {
        await using DataContext dataContext = await dataContextFactory.CreateDbContextAsync(ct);

        Challenge challenge = await GetChallengeByIdAsync(challengeId, ct)
            ?? throw new Exception($"Challenge ID {challengeId} not found");

        int[] athleteIds = [.. challenge.Athletes!.Select(a => a.Id)];

        Activity[] activities = await dataContext.Activities
            .Where(activity =>
                activity.Details.StartDate >= challenge.Start &&
                activity.Details.StartDate < challenge.End &&
                (challenge.ActivityTypes.Count == 0 || challenge.ActivityTypes.Contains(activity.Details.Type)) &&
                athleteIds.Contains(activity.AthleteId))
            .ToArrayAsync(ct);

        bool attribute = activities.Select(a => a.Details.DeviceName).Any(d => d != null && d.Contains("Garmin"));

        AthleteScore[] scores = [.. challenge.Athletes!
            .Select(
                athlete => new
                {
                    Athlete = athlete.Name,
                    Activities = activities.Where(activity => activity.AthleteId == athlete.Id),
                })
            .Select(
                a => new
                {
                    a.Athlete,
                    Sum = challenge.Measurement switch
                    {
                        ChallengeMeasurement.Distance => a.Activities.Select(x => x.Details.DistanceInMeters).Sum(),
                        ChallengeMeasurement.Time => a.Activities.Select(x => x.Details.MovingTimeInSeconds).Sum(),
                        ChallengeMeasurement.Elevation => a.Activities.Select(x => x.Details.TotalElevationGain).Sum(),
                        _ => 0,
                    },
                })
            .OrderByDescending(s => s.Sum)
            .Select(
                s =>
                {
                    string score = challenge.Measurement switch
                    {
                        ChallengeMeasurement.Distance => $"{s.Sum / 1000:0.##} km",
                        ChallengeMeasurement.Time => TimeSpan.FromSeconds(s.Sum).ToString("g"),
                        ChallengeMeasurement.Elevation => $"{s.Sum:0.##} m",
                        _ => s.Sum.ToString(CultureInfo.InvariantCulture),
                    };

                    float scoreValue = challenge.Measurement switch
                    {
                        ChallengeMeasurement.Distance => s.Sum / 1000,
                        ChallengeMeasurement.Time => (float)TimeSpan.FromSeconds(s.Sum).TotalHours,
                        ChallengeMeasurement.Elevation => s.Sum,
                        _ => s.Sum,
                    };

                    return new AthleteScore(s.Athlete, score, scoreValue);
                })];

        return new ScoreBoard(scores, attribute);
    }

    public async Task<Athlete?> DrawChallengeWinnerAsync(int challengeId, CancellationToken ct = default)
    {
        await using DataContext dataContext = await dataContextFactory.CreateDbContextAsync(ct);

        Challenge? challenge = await dataContext.Challenges
            .Include(c => c.Athletes)!
            .ThenInclude(a => a.Activities)
            .AsTracking()
            .AsSplitQuery()
            .FirstOrDefaultAsync(c => c.Id == challengeId, ct);

        if (challenge == null) return null;

        List<(Athlete Athlete, int Tickets)> tickets = [];

        foreach (Athlete athlete in challenge.Athletes ?? [])
        {
            int athleteTickets = 0;
            if (athlete.Activities == null) continue;
            IEnumerable<Activity> challengeActivities = athlete.Activities.Where(
                a =>
                    (challenge.ActivityTypes.Count == 0 || challenge.ActivityTypes.Contains(a.Details.Type)) &&
                    a.Details.StartDate >= challenge.Start &&
                    a.Details.StartDate <= challenge.End);

            foreach (Activity? activity in challengeActivities)
                switch (challenge.Measurement)
                {
                    case ChallengeMeasurement.Distance:
                        athleteTickets += (int)(activity.Details.DistanceInMeters / 1000); // 1 km = 1 ticket
                        break;
                    case ChallengeMeasurement.Elevation:
                        athleteTickets += (int)activity.Details.TotalElevationGain; // 1 m = 1 ticket
                        break;
                    case ChallengeMeasurement.Time:
                        athleteTickets += activity.Details.MovingTimeInSeconds / 1800; // 30 min = 1 ticket
                        break;
                    default:
                        athleteTickets += 0;
                        break;
                }

            if (athleteTickets > 0) tickets.Add((athlete, athleteTickets));
        }

        int totalTickets = tickets.Sum(t => t.Tickets);
        int randomNumber = new Random().Next(1, totalTickets + 1);

        int currentMax = 0;
        foreach ((Athlete Athlete, int Tickets) in tickets)
        {
            currentMax += Tickets;
            if (randomNumber > currentMax) continue;

            challenge.WinnerId = Athlete.Id;
            await dataContext.UpdateChallengeAsync(challenge, CancellationToken.None);

            _challengeFeed.OnNext(new FeedUpdate<Challenge> { Item = challenge, Action = FeedAction.Update });

            return Athlete;
        }

        return null;
    }

    public async Task ClearChallengeWinnerAsync(int challengeId, CancellationToken ct = default)
    {
        await using DataContext dataContext = await dataContextFactory.CreateDbContextAsync(ct);

        Challenge? challenge = await dataContext.Challenges.FirstOrDefaultAsync(c => c.Id == challengeId, ct);
        if (challenge == null) return;

        challenge.WinnerId = null;
        await dataContext.UpdateChallengeAsync(challenge, CancellationToken.None);
    }
}
