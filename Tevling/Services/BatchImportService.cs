namespace Tevling.Services;

public class BatchImportService(
    IAthleteService athleteService,
    IActivityService activityService,
    ILogger<BatchImportService> logger)
    : BackgroundService
{
    private readonly TimeOnly _startTime = new(3, 0); // 3 AM
    private readonly TimeSpan _importInterval = TimeSpan.FromDays(1); // Every night

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            TimeSpan timeUntilNextRun = _startTime - TimeOnly.FromDateTime(DateTime.Now);
            await Task.Delay(timeUntilNextRun, stoppingToken);

            await BatchImport(_importInterval, stoppingToken);
        }
    }

    private async Task BatchImport(TimeSpan timeSpan, CancellationToken cancellationToken)
    {
        DateTimeOffset from = DateTimeOffset.Now - timeSpan;
        Athlete[] athletes = await athleteService.GetAthletesAsync(filter: null, paging: null, cancellationToken);

        logger.LogInformation("Importing activities for {AthleteCount} athletes from {From}", athletes.Length, from);

        foreach (Athlete athlete in athletes)
        {
            try
            {
                await activityService.ImportActivitiesForAthleteAsync(athlete.Id, from, cancellationToken);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error importing activities for athlete ID {AthleteId}", athlete.Id);
            }
        }

        logger.LogInformation("Batch import done");
    }
}
