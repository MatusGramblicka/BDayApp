using Cronos;

namespace BDayServer.HostedService;

/// <summary>
/// Runs <see cref="DoWorkAsync"/> at the occurrences of a cron expression.
/// <list type="bullet">
/// <item>A failing run is logged and the schedule goes on; it is not recorded as completed, so the next start of the
/// app retries it.</item>
/// <item>The next occurrence is computed from the scheduled one, not from the time a run ended, so a long run neither
/// skips nor repeats an occurrence.</item>
/// <item>Completed occurrences are recorded in a <see cref="CronJobStateFile"/>. At startup the most recent occurrence
/// is run once if it lies within <see cref="CatchUpWindow"/> and is not recorded as completed (the app sleeps on the
/// App Service Free tier and misses its runs). Without a state file nothing is caught up, so a first deployment does not
/// send e-mails at an unexpected time. An occurrence that is already recorded is never run again.</item>
/// </list>
/// </summary>
public abstract class CronJobService : BackgroundService
{
    /// <summary>How old a missed occurrence may be to still be caught up at startup.</summary>
    public static readonly TimeSpan CatchUpWindow = TimeSpan.FromHours(24);

    // Task.Delay accepts at most ~49 days; longer waits are split, and the clock is read again after each part.
    private static readonly TimeSpan MaxDelay = TimeSpan.FromDays(1);

    private readonly CronExpression _expression;
    private readonly CronJobStateFile _stateFile;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger _logger;

    protected CronJobService(string cronExpression, TimeZoneInfo? timeZone, CronJobStateFile stateFile,
        TimeProvider timeProvider, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(stateFile);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _expression = CronExpression.Parse(cronExpression);
        TimeZone = timeZone ?? TimeZoneInfo.Utc;
        _stateFile = stateFile;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <summary>The time zone the cron expression is evaluated in.</summary>
    protected TimeZoneInfo TimeZone { get; }

    /// <summary>The clock the schedule runs on.</summary>
    protected TimeProvider Clock => _timeProvider;

    /// <summary>The logger passed in by the job.</summary>
    protected ILogger Logger => _logger;

    private string JobName => GetType().Name;

    /// <summary>
    /// Does the work of one occurrence. Throw when the run as a whole failed: the occurrence is then not recorded as
    /// completed, so the next start of the app runs it again (if it is still within <see cref="CatchUpWindow"/>).
    /// </summary>
    public abstract Task DoWorkAsync(DateTimeOffset occurrence, CancellationToken cancellationToken);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("{Job} started; completed runs are recorded in {StateFile}", JobName,
                _stateFile.FilePath);

            var lastScheduled = await CatchUpAsync(stoppingToken);

            while (true)
            {
                var next = _expression.GetNextOccurrence(lastScheduled, TimeZone);
                if (next is null)
                {
                    _logger.LogWarning("{Job}: the cron expression has no further occurrences", JobName);
                    return;
                }

                _logger.LogInformation("{Job}: next run at {Occurrence:u}", JobName, next.Value);
                await WaitUntilAsync(next.Value, stoppingToken);

                var occurrence = LatestDueOccurrence(next.Value);
                await RunAsync(occurrence, stoppingToken);
                lastScheduled = occurrence;
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // The app is shutting down.
        }
        catch (Exception ex)
        {
            // Never rethrow: an exception escaping ExecuteAsync stops the whole web app.
            _logger.LogCritical(ex, "{Job}: the scheduler stopped unexpectedly; nothing runs until the app restarts",
                JobName);
        }
    }

    /// <summary>Runs the most recent occurrence if it was missed; returns the occurrence the schedule continues after.</summary>
    private async Task<DateTimeOffset> CatchUpAsync(CancellationToken stoppingToken)
    {
        var now = _timeProvider.GetUtcNow();
        var mostRecent = _expression.GetPreviousOccurrence(now, TimeZone, inclusive: true);
        if (mostRecent is null)
            return now;

        if (now - mostRecent.Value > CatchUpWindow)
            return mostRecent.Value;

        var lastCompleted = await _stateFile.ReadLastCompletedOccurrenceAsync(stoppingToken);
        if (lastCompleted is null)
        {
            _logger.LogInformation("{Job}: no completed run is recorded, so the occurrence {Occurrence:u} is not " +
                                   "caught up; waiting for the next one", JobName, mostRecent.Value);
            return mostRecent.Value;
        }

        if (lastCompleted.Value < mostRecent.Value)
        {
            _logger.LogInformation("{Job}: catching up the missed occurrence {Occurrence:u} (last completed: " +
                                   "{LastCompleted:u})", JobName, mostRecent.Value, lastCompleted.Value);
            await RunAsync(mostRecent.Value, stoppingToken);
        }

        return mostRecent.Value;
    }

    private async Task WaitUntilAsync(DateTimeOffset dueTime, CancellationToken stoppingToken)
    {
        for (var remaining = dueTime - _timeProvider.GetUtcNow();
             remaining > TimeSpan.Zero;
             remaining = dueTime - _timeProvider.GetUtcNow())
        {
            await Task.Delay(remaining < MaxDelay ? remaining : MaxDelay, _timeProvider, stoppingToken);
        }
    }

    /// <summary>
    /// Normally <paramref name="next"/>. If the app fell behind by more than one occurrence (a run longer than the
    /// interval, a suspended machine), only the latest due occurrence runs instead of one run per missed occurrence.
    /// </summary>
    private DateTimeOffset LatestDueOccurrence(DateTimeOffset next)
    {
        var latest = _expression.GetPreviousOccurrence(_timeProvider.GetUtcNow(), TimeZone, inclusive: true);
        if (latest is null || latest.Value <= next)
            return next;

        _logger.LogWarning("{Job} fell behind: skipping the occurrences from {Skipped:u} on and running only " +
                           "{Occurrence:u}", JobName, next, latest.Value);
        return latest.Value;
    }

    private async Task RunAsync(DateTimeOffset occurrence, CancellationToken stoppingToken)
    {
        var lastCompleted = await _stateFile.ReadLastCompletedOccurrenceAsync(stoppingToken);
        if (lastCompleted >= occurrence)
        {
            _logger.LogInformation("{Job}: the occurrence {Occurrence:u} is already completed; not running it again",
                JobName, occurrence);
            return;
        }

        _logger.LogInformation("{Job}: running the occurrence {Occurrence:u}", JobName, occurrence);
        var started = _timeProvider.GetTimestamp();
        try
        {
            await DoWorkAsync(occurrence, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogWarning("{Job}: the run for {Occurrence:u} was cancelled by the shutdown and is not recorded " +
                               "as completed", JobName, occurrence);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Job}: the run for {Occurrence:u} failed; it is not recorded as completed, so the " +
                                 "next start of the app retries it", JobName, occurrence);
            return;
        }

        _logger.LogInformation("{Job}: completed the occurrence {Occurrence:u} in {Elapsed}", JobName, occurrence,
            _timeProvider.GetElapsedTime(started));
        await _stateFile.WriteLastCompletedOccurrenceAsync(occurrence);
    }
}
