using System.Reactive.Subjects;
using Microsoft.EntityFrameworkCore;

namespace Tevling.Services;

public class ChallengeService : IChallengeService
{
    private readonly Subject<FeedUpdate<Challenge>> _challengeFeed = new();
    private readonly IDbContextFactory<DataContext> _dataContextFactory;
    private readonly ILogger<ChallengeService> _logger;

    public ChallengeService(
        ILogger<ChallengeService> logger,
        IDbContextFactory<DataContext> dataContextFactory)
    {
        _logger = logger;
        _dataContextFactory = dataContextFactory;
    }

    public async Task<Challenge?> GetChallengeByIdAsync(int challengeId, CancellationToken ct = default)
    {
        await using DataContext dataContext = await _dataContextFactory.CreateDbContextAsync(ct);

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
        await using DataContext dataContext = await _dataContextFactory.CreateDbContextAsync(ct);

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
                c => string.IsNullOrWhiteSpace(filter.SearchText)
                    // TODO: Use EF.Functions.ILike when switching to PostgreSQL
                    //       to keep the search text case-insensitive
                    ||
                    EF.Functions.Like(c.Title, $"%{filter.SearchText}%"))
            .OrderByDescending(challenge => challenge.Start)
            .ThenBy(challenge => challenge.Title)
            .ThenBy(challenge => challenge.Id)
            .If(paging != null, x => x.Skip(paging!.Value.Page * paging!.Value.PageSize), x => x)
            .If(paging != null, x => x.Take(paging!.Value.PageSize))
            .ToArrayAsync(ct);

        return challenges;
    }

    public IObservable<FeedUpdate<Challenge>> GetChallengeFeed()
    {
        return _challengeFeed.AsObservable();
    }

    public async Task<Challenge> CreateChallengeAsync(ChallengeFormModel newChallenge, CancellationToken ct = default)
    {
        await using DataContext dataContext = await _dataContextFactory.CreateDbContextAsync(ct);

        _logger.LogInformation("Adding new challenge: {Title}", newChallenge.Title);

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

    public async Task<Challenge> UpdateChallengeAsync(
        int challengeId,
        ChallengeFormModel editChallenge,
        CancellationToken ct = default)
    {
        await using DataContext dataContext = await _dataContextFactory.CreateDbContextAsync(ct);

        Challenge challenge = await dataContext.Challenges
                .Include(c => c.Athletes)
                .Include(c => c.CreatedBy)
                .Include(c => c.InvitedAthletes)
                .AsSplitQuery()
                .AsTracking()
                .FirstOrDefaultAsync(c => c.Id == challengeId, ct) ??
            throw new Exception($"Unknown challenge ID {challengeId}");

        _logger.LogInformation("Updating challenge ID {ChallengeId}", challengeId);


        bool stillInvited(Athlete a)
        {
            return editChallenge.InvitedAthletes.Any(i => i.Id == a.Id);
        }

        IEnumerable<Athlete> newInvites = editChallenge.InvitedAthletes
            .Where(i => !challenge.InvitedAthletes!.Any(ii => ii.Id == i.Id));

        challenge.InvitedAthletes = challenge.InvitedAthletes!
            .Where(stillInvited)
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
        await using DataContext dataContext = await _dataContextFactory.CreateDbContextAsync(ct);

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
        await using DataContext dataContext = await _dataContextFactory.CreateDbContextAsync(ct);

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
        await using DataContext dataContext = await _dataContextFactory.CreateDbContextAsync(ct);

        Challenge challenge = await dataContext.Challenges
                .Include(c => c.Athletes) // TODO: Is this necessary?
                .FirstOrDefaultAsync(c => c.Id == challengeId, ct) ??
            throw new Exception($"Unknown challenge ID {challengeId}");

        _logger.LogInformation("Deleting challenge ID {ChallengeId}", challengeId);
        _ = await dataContext.RemoveChallengeAsync(challenge, ct);

        _challengeFeed.OnNext(new FeedUpdate<Challenge> { Item = challenge, Action = FeedAction.Delete });
    }

    public async Task<ScoreBoard> GetScoreBoardAsync(int challengeId, CancellationToken ct = default)
    {
        await using DataContext dataContext = await _dataContextFactory.CreateDbContextAsync(ct);

        var result = await dataContext.Challenges
            .Select(
                c => new
                {
                    Challenge = c,
                    Athletes = c.Athletes!
                        .Select(
                            a => new
                            {
                                Athlete = a.Name,
                                /*
                                This is not possible with the SQLite provider as it has no support for the APPLY
                                operation which EFCore requires.
                                Leaving the idea in here in case a different DB provider is used in the future.

                                Score = a.Activities!
                                    .Where(a => c.ActivityTypes.Length == 0 || c.ActivityTypes.Contains(a.Details.Type))
                                    .Select(a => a.Details.DistanceInMeters).Sum()
                                */
                                a.Activities,
                            }),
                })
            .AsSplitQuery()
            .FirstAsync(x => x.Challenge.Id == challengeId, ct);

        AthleteScore[] scores = result.Athletes
            .Select(
                a => new
                {
                    a.Athlete,
                    Activities = a.Activities!
                        .Where(
                            a => a.Details.StartDate >= result.Challenge.Start &&
                                a.Details.StartDate < result.Challenge.End &&
                                (result.Challenge.ActivityTypes.Count == 0 ||
                                    result.Challenge.ActivityTypes.Contains(a.Details.Type))),
                })
            .Select(
                a => new
                {
                    a.Athlete,
                    Sum = result.Challenge.Measurement switch
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
                    string score = result.Challenge.Measurement switch
                    {
                        ChallengeMeasurement.Distance => $"{s.Sum / 1000:0.##} km",
                        ChallengeMeasurement.Time => TimeSpan.FromSeconds(s.Sum).ToString("g"),
                        ChallengeMeasurement.Elevation => $"{s.Sum:0.##} m",
                        _ => s.Sum.ToString(),
                    };

                    return new AthleteScore(s.Athlete, score);
                })
            .ToArray();

        return new ScoreBoard(scores);
    }

    public async Task<Athlete?> DrawChallengeWinnerAsync(int challengeId, CancellationToken ct = default)
    {
        await using DataContext dataContext = await _dataContextFactory.CreateDbContextAsync(ct);

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
        await using DataContext dataContext = await _dataContextFactory.CreateDbContextAsync(ct);

        Challenge? challenge = await dataContext.Challenges.FirstOrDefaultAsync(c => c.Id == challengeId, ct);
        if (challenge == null) return;

        challenge.WinnerId = null;
        await dataContext.UpdateChallengeAsync(challenge, CancellationToken.None);
    }
}
