using System.Reactive.Subjects;
using Microsoft.EntityFrameworkCore;

namespace Tevling.Services;

public class ChallengeService : IChallengeService
{
    private readonly ILogger<ChallengeService> _logger;
    private readonly IDbContextFactory<DataContext> _dataContextFactory;
    private readonly Subject<FeedUpdate<Challenge>> _challengeFeed = new();

    public ChallengeService(
        ILogger<ChallengeService> logger,
        IDbContextFactory<DataContext> dataContextFactory)
    {
        _logger = logger;
        _dataContextFactory = dataContextFactory;
    }

    public async Task<Challenge?> GetChallengeByIdAsync(int challengeId, CancellationToken ct = default)
    {
        using DataContext dataContext = await _dataContextFactory.CreateDbContextAsync(ct);

        Challenge? challenge = await dataContext.Challenges
            .Include(c => c.Athletes)
            .Include(c => c.InvitedAthletes)
            .Include(c => c.CreatedBy)
            .AsSplitQuery()
            .FirstOrDefaultAsync(c => c.Id == challengeId, ct);

        return challenge;
    }

    public async Task<Challenge[]> GetChallengesAsync(int currentAthleteId, ChallengeFilter filter, int pageSize, int page = 0, CancellationToken ct = default)
    {
        using DataContext dataContext = await _dataContextFactory.CreateDbContextAsync(ct);

        Challenge[] challenges = await dataContext.Challenges
            .Include(challenge => challenge.Athletes)
            .Include(challenge => challenge.InvitedAthletes)
            .Include(challenge => challenge.CreatedBy)
            .AsSplitQuery()
            .Where(challenge => !challenge.IsPrivate
                || challenge.InvitedAthletes!.Any(a => a.Id == currentAthleteId)
                || challenge.CreatedById == currentAthleteId)
            .Where(challenge => !filter.ByAthleteId.HasValue
                || challenge.Athletes!.Any(athlete => athlete.Id == filter.ByAthleteId.Value) == true
                || challenge.CreatedById == filter.ByAthleteId.Value)
            // TODO: Filter out outdated challenges when DB supports it
            //       (Time to switch to PostgreSQL?)
            // .Where(challenge => filter.IncludeOutdatedChallenges
            //     || challenge.End.UtcDateTime.Date >= DateTimeOffset.UtcNow.Date)
            .Where(c => string.IsNullOrWhiteSpace(filter.SearchText)
                // TODO: Use EF.Functions.ILike when switching to PostgreSQL
                //       to keep the search text case-insensitive
                || EF.Functions.Like(c.Title, $"%{filter.SearchText}%"))
            .OrderByDescending(challenge => challenge.Start)
            .ThenBy(challenge => challenge.Title)
            .Skip(pageSize * page)
            .Take(pageSize)
            .ToArrayAsync(ct);

        return challenges;
    }

    public IObservable<FeedUpdate<Challenge>> GetChallengeFeed()
    {
        return _challengeFeed.AsObservable();
    }

    public async Task<Challenge> CreateChallengeAsync(ChallengeFormModel newChallenge, CancellationToken ct = default)
    {
        using DataContext dataContext = await _dataContextFactory.CreateDbContextAsync(ct);

        _logger.LogInformation("Adding new challenge: {Title}", newChallenge.Title);

        if (newChallenge.InvitedAthletes != null)
        {
            foreach (Athlete athlete in newChallenge.InvitedAthletes)
            {
                await dataContext.Entry(athlete).ReloadAsync(ct);
            }
        }

        Challenge challenge = await dataContext.AddChallengeAsync(new Challenge()
        {
            Title = newChallenge.Title,
            Description = newChallenge.Description,
            Start = newChallenge.Start,
            End = newChallenge.End,
            Measurement = newChallenge.Measurement,
            ActivityTypes = newChallenge.ActivityTypes,
            IsPrivate = newChallenge.IsPrivate,
            Created = DateTimeOffset.Now,
            CreatedById = newChallenge.CreatedBy,
            InvitedAthletes = newChallenge.InvitedAthletes,
        }, ct);

        await dataContext.Entry(challenge).Collection(c => c.Athletes!).LoadAsync(ct);

        _challengeFeed.OnNext(new FeedUpdate<Challenge> { Item = challenge, Action = FeedAction.Create });

        return challenge;
    }

    public async Task<Challenge> UpdateChallengeAsync(int challengeId, ChallengeFormModel editChallenge,
        CancellationToken ct = default)
    {
        using DataContext dataContext = await _dataContextFactory.CreateDbContextAsync(ct);

        Challenge challenge = await dataContext.Challenges
            .Include(c => c.Athletes)
            .Include(c => c.CreatedBy)
            .AsTracking()
            .FirstOrDefaultAsync(c => c.Id == challengeId, ct)
                ?? throw new Exception($"Unknown challenge ID {challengeId}");

        _logger.LogInformation("Updating challenge ID {ChallengeId}", challengeId);

        challenge.Title = editChallenge.Title;
        challenge.Description = editChallenge.Description;
        challenge.Start = editChallenge.Start;
        challenge.End = editChallenge.End;
        challenge.Measurement = editChallenge.Measurement;
        challenge.ActivityTypes = editChallenge.ActivityTypes;
        challenge.IsPrivate = editChallenge.IsPrivate;

        challenge = await dataContext.UpdateChallengeAsync(challenge, CancellationToken.None);

        _challengeFeed.OnNext(new FeedUpdate<Challenge> { Item = challenge, Action = FeedAction.Update });

        return challenge;
    }

    public async Task<Challenge> InviteAthleteAsync(int athleteId, int challengeId, CancellationToken ct = default)
    {
        using DataContext dataContext = await _dataContextFactory.CreateDbContextAsync(ct);

        Challenge challenge = await dataContext.Challenges
            .Include(c => c.Athletes)
            .Include(c => c.CreatedBy)
            .Include(c => c.InvitedAthletes)
            .AsTracking()
            .FirstOrDefaultAsync(c => c.Id == challengeId, ct)
                ?? throw new Exception($"Challenge ID {challengeId} not found");

        Athlete athlete = await dataContext.Athletes
            .AsTracking()
            .FirstOrDefaultAsync(a => a.Id == athleteId, ct)
                ?? throw new Exception($"Athlete ID {athleteId} not found");

        challenge.InvitedAthletes!.Add(athlete);

        await dataContext.UpdateChallengeAsync(challenge, ct);

        _challengeFeed.OnNext(new FeedUpdate<Challenge> { Item = challenge, Action = FeedAction.Update });

        return challenge;
    }

    public async Task<Challenge> JoinChallengeAsync(int athleteId, int challengeId, CancellationToken ct = default)
    {
        using DataContext dataContext = await _dataContextFactory.CreateDbContextAsync(ct);

        Challenge challenge = await dataContext.Challenges
            .Include(c => c.Athletes)
            .Include(c => c.CreatedBy)
            .AsTracking()
            .FirstOrDefaultAsync(c => c.Id == challengeId, ct)
                ?? throw new Exception($"Challenge ID {challengeId} not found");

        Athlete athlete = await dataContext.Athletes
            .AsTracking()
            .FirstOrDefaultAsync(a => a.Id == athleteId, ct)
                ?? throw new Exception($"Athlete ID {athleteId} not found");

        challenge.Athletes!.Add(athlete);

        await dataContext.UpdateChallengeAsync(challenge, ct);

        _challengeFeed.OnNext(new FeedUpdate<Challenge> { Item = challenge, Action = FeedAction.Update });

        return challenge;
    }

    public async Task<Challenge> LeaveChallengeAsync(int athleteId, int challengeId, CancellationToken ct = default)
    {
        using DataContext dataContext = await _dataContextFactory.CreateDbContextAsync(ct);

        Challenge challenge = await dataContext.Challenges
            .Include(c => c.Athletes)
            .Include(c => c.CreatedBy)
            .AsTracking()
            .FirstOrDefaultAsync(c => c.Id == challengeId, ct)
                ?? throw new Exception($"Challenge ID {challengeId} not found");

        Athlete? athlete = challenge.Athletes!.FirstOrDefault(a => a.Id == athleteId);

        if (athlete != null)
        {
            challenge.Athletes!.Remove(athlete);
        }

        await dataContext.UpdateChallengeAsync(challenge, ct);

        _challengeFeed.OnNext(new FeedUpdate<Challenge> { Item = challenge, Action = FeedAction.Update });

        return challenge;
    }

    public async Task DeleteChallengeAsync(int challengeId, CancellationToken ct = default)
    {
        using DataContext dataContext = await _dataContextFactory.CreateDbContextAsync(ct);

        Challenge challenge = await dataContext.Challenges
            .Include(c => c.Athletes) // TODO: Is this necessary?
            .FirstOrDefaultAsync(c => c.Id == challengeId, ct)
                ?? throw new Exception($"Unknown challenge ID {challengeId}");

        _logger.LogInformation("Deleting challenge ID {ChallengeId}", challengeId);
        _ = await dataContext.RemoveChallengeAsync(challenge, ct);

        _challengeFeed.OnNext(new FeedUpdate<Challenge> { Item = challenge, Action = FeedAction.Delete });
    }

    public async Task<ScoreBoard> GetScoreBoardAsync(int challengeId, CancellationToken ct = default)
    {
        using DataContext dataContext = await _dataContextFactory.CreateDbContextAsync(ct);

        var result = await dataContext.Challenges
            .Select(c => new
            {
                Challenge = c,
                Athletes = c.Athletes!
                    .Select(a => new
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
                        a.Activities
                    })
            })
            .AsSplitQuery()
            .FirstAsync(x => x.Challenge.Id == challengeId, ct);

        AthleteScore[] scores = result.Athletes
            .Select(a => new
            {
                a.Athlete,
                Activities = a.Activities!
                    .Where(a => a.Details.StartDate >= result.Challenge.Start
                            && a.Details.StartDate < result.Challenge.End
                            && (result.Challenge.ActivityTypes.Length == 0
                            || result.Challenge.ActivityTypes.Contains(a.Details.Type)))
            })
            .Select(a => new
            {
                a.Athlete,
                Sum = result.Challenge.Measurement == ChallengeMeasurement.Distance
                    ? a.Activities.Select(x => x.Details.DistanceInMeters).Sum()
                    : a.Activities.Select(x => x.Details.ElapsedTimeInSeconds).Sum(),
            })
            .OrderByDescending(s => s.Sum)
            .Select(s =>
            {
                string score = result.Challenge.Measurement == ChallengeMeasurement.Distance
                    ? $"{(s.Sum / 1000):0.##} km"
                    : TimeSpan.FromSeconds(s.Sum).ToString("g");

                return new AthleteScore(s.Athlete, score);
            })
            .ToArray();

        return new ScoreBoard(scores);
    }
}
