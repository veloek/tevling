using System.Reactive.Linq;
using System.Reactive.Subjects;
using Microsoft.EntityFrameworkCore;
using Spur.Data;
using Spur.Model;

namespace Spur.Services;

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
            .Include(c => c.CreatedBy)
            .FirstOrDefaultAsync(c => c.Id == challengeId, ct);

        return challenge;
    }

    public async Task<Challenge[]> GetChallengesAsync(int pageSize, int page = 0, CancellationToken ct = default)
    {
        using DataContext dataContext = await _dataContextFactory.CreateDbContextAsync(ct);

        Challenge[] challenges = await dataContext.Challenges
            .Include(challenge => challenge.Athletes)
            .Include(challenge => challenge.CreatedBy)
            .OrderByDescending(challenge => challenge.Start)
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

        Challenge challenge = await dataContext.AddChallengeAsync(new Challenge()
        {
            Title = newChallenge.Title,
            Description = newChallenge.Description,
            Start = newChallenge.Start,
            End = newChallenge.End,
            Measurement = newChallenge.Measurement,
            ActivityTypes = newChallenge.ActivityTypes,
            Created = DateTimeOffset.Now,
            CreatedById = newChallenge.CreatedBy,
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

        challenge = await dataContext.UpdateChallengeAsync(challenge, CancellationToken.None);

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

    public async Task DeleteChallengeAsync(int challengeId, CancellationToken ct = default)
    {
        using DataContext dataContext = await _dataContextFactory.CreateDbContextAsync(ct);

        Challenge challenge = await dataContext.Challenges
            .Include(c => c.Athletes)
            .FirstOrDefaultAsync(c => c.Id == challengeId, ct)
                ?? throw new Exception($"Unknown challenge ID {challengeId}");

        _logger.LogInformation($"Deleting challenge ID {challengeId}");
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
                Score = a.Activities!
                    .Where(a => result.Challenge.ActivityTypes.Length == 0
                            || result.Challenge.ActivityTypes.Contains(a.Details.Type))
                    .Select(a => a.Details.DistanceInMeters)
                    .Sum()
            })
            .OrderByDescending(s => s.Score)
            .Select(a => new AthleteScore(a.Athlete, (int)a.Score))
            .ToArray();

        return new ScoreBoard(scores);
    }
}
