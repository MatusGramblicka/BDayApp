using System.Threading.Channels;
using BDayServer.HostedService;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

namespace BDayServer.UnitTests.HostedService;

/// <summary>
/// Drives the scheduler loop with a fake clock. "Waiting for the next occurrence" is observed as the creation of the
/// loop's delay timer, so the tests never depend on real time passing.
/// </summary>
public sealed class CronJobServiceTests : IAsyncLifetime
{
    private const string DailyAtNoon = "0 12 * * *";

    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);
    private static readonly DateTimeOffset Nov21Noon = new(2026, 11, 21, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Nov22Noon = Nov21Noon.AddDays(1);
    private static readonly DateTimeOffset Nov23Noon = Nov21Noon.AddDays(2);

    private readonly string _directory =
        Path.Combine(Path.GetTempPath(), "BDayServer.UnitTests", nameof(CronJobServiceTests), Guid.NewGuid().ToString("N"));

    private readonly List<TestJob> _jobs = [];
    private readonly CronJobStateFile _stateFile;

    public CronJobServiceTests()
    {
        _stateFile = new CronJobStateFile(Path.Combine(_directory, "last-run.txt"),
            NullLogger<CronJobStateFile>.Instance);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        foreach (var job in _jobs)
        {
            await job.StopAsync(CancellationToken.None);
            job.Dispose();
        }

        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }

    private async Task<TestJob> StartJobAsync(ObservableTimeProvider time, Func<DateTimeOffset, Task>? onRun = null,
        string cron = DailyAtNoon)
    {
        var job = new TestJob(cron, _stateFile, time, onRun ?? (_ => Task.CompletedTask));
        _jobs.Add(job);
        await job.StartAsync(CancellationToken.None);
        return job;
    }

    private Task<DateTimeOffset?> LastCompletedAsync() => _stateFile.ReadLastCompletedOccurrenceAsync();

    [Fact]
    public async Task RunsAtEachOccurrence_AndRecordsIt()
    {
        await _stateFile.WriteLastCompletedOccurrenceAsync(Nov21Noon);
        var time = new ObservableTimeProvider(Nov22Noon.AddHours(-2));

        var job = await StartJobAsync(time);
        await time.WaitForDelayAsync();
        job.HasUnreadRun.Should().BeFalse("nothing was missed and noon has not come yet");

        time.Advance(TimeSpan.FromHours(2));
        (await job.NextRunAsync()).Should().Be(new Run(Nov22Noon, At: Nov22Noon));

        await time.WaitForDelayAsync();
        (await LastCompletedAsync()).Should().Be(Nov22Noon);

        time.Advance(TimeSpan.FromDays(1));
        (await job.NextRunAsync()).Should().Be(new Run(Nov23Noon, At: Nov23Noon));
        await time.WaitForDelayAsync();
        (await LastCompletedAsync()).Should().Be(Nov23Noon);
    }

    [Fact]
    public async Task Startup_MissedOccurrenceWithin24Hours_IsCaughtUpOnce()
    {
        await _stateFile.WriteLastCompletedOccurrenceAsync(Nov21Noon);
        var startup = Nov22Noon.AddHours(6); // the app slept through noon and wakes up at 18:00
        var time = new ObservableTimeProvider(startup);

        var job = await StartJobAsync(time);

        (await job.NextRunAsync()).Should().Be(new Run(Nov22Noon, At: startup));
        await time.WaitForDelayAsync();
        job.HasUnreadRun.Should().BeFalse();
        (await LastCompletedAsync()).Should().Be(Nov22Noon);

        time.Advance(TimeSpan.FromHours(18));
        (await job.NextRunAsync()).Occurrence.Should().Be(Nov23Noon);
    }

    [Fact]
    public async Task Startup_WithoutStateFile_DoesNotCatchUp()
    {
        var time = new ObservableTimeProvider(Nov22Noon.AddHours(6));

        var job = await StartJobAsync(time);
        await time.WaitForDelayAsync();

        job.HasUnreadRun.Should().BeFalse("a first deployment must not send e-mails at an unexpected time");
        File.Exists(_stateFile.FilePath).Should().BeFalse();

        time.Advance(TimeSpan.FromHours(18));
        (await job.NextRunAsync()).Occurrence.Should().Be(Nov23Noon);
        await time.WaitForDelayAsync();
        (await LastCompletedAsync()).Should().Be(Nov23Noon);
    }

    [Fact]
    public async Task Startup_MostRecentOccurrenceAlreadyCompleted_IsNotRunAgain()
    {
        await _stateFile.WriteLastCompletedOccurrenceAsync(Nov22Noon);
        var time = new ObservableTimeProvider(Nov22Noon.AddMinutes(30)); // restarted after the run

        var job = await StartJobAsync(time);
        await time.WaitForDelayAsync();

        job.HasUnreadRun.Should().BeFalse();

        time.Advance(TimeSpan.FromHours(23.5));
        (await job.NextRunAsync()).Occurrence.Should().Be(Nov23Noon);
    }

    [Fact]
    public async Task Startup_MissedOccurrenceOlderThan24Hours_IsNotCaughtUp()
    {
        // Mondays at noon. 2026-11-22 is a Sunday: the last occurrence (Monday the 16th) is 6 days old.
        await _stateFile.WriteLastCompletedOccurrenceAsync(new DateTimeOffset(2026, 11, 9, 12, 0, 0, TimeSpan.Zero));
        var time = new ObservableTimeProvider(Nov22Noon);

        var job = await StartJobAsync(time, cron: "0 12 * * 1");
        await time.WaitForDelayAsync();

        job.HasUnreadRun.Should().BeFalse();

        time.Advance(TimeSpan.FromDays(1));
        (await job.NextRunAsync()).Occurrence.Should().Be(Nov23Noon);
    }

    [Fact]
    public async Task ScheduledOccurrenceAlreadyRecorded_IsSkipped()
    {
        await _stateFile.WriteLastCompletedOccurrenceAsync(Nov21Noon);
        var time = new ObservableTimeProvider(Nov22Noon.AddHours(-1));
        var job = await StartJobAsync(time);
        await time.WaitForDelayAsync();

        // Meanwhile another instance of the app (e.g. during a restart) completed the occurrence.
        await _stateFile.WriteLastCompletedOccurrenceAsync(Nov22Noon);
        time.Advance(TimeSpan.FromHours(1));
        await time.WaitForDelayAsync();

        job.HasUnreadRun.Should().BeFalse();
    }

    [Fact]
    public async Task FailingRun_DoesNotStopTheSchedule_AndIsNotRecorded()
    {
        await _stateFile.WriteLastCompletedOccurrenceAsync(Nov21Noon);
        var time = new ObservableTimeProvider(Nov22Noon.AddHours(-1));
        var job = await StartJobAsync(time, occurrence => occurrence == Nov22Noon
            ? throw new InvalidOperationException("Database unreachable")
            : Task.CompletedTask);
        await time.WaitForDelayAsync();

        time.Advance(TimeSpan.FromHours(1));
        (await job.NextRunAsync()).Occurrence.Should().Be(Nov22Noon);
        await time.WaitForDelayAsync();

        (await LastCompletedAsync()).Should().Be(Nov21Noon, "a failed run must be retried by the next start");
        job.ExecuteTask!.IsCompleted.Should().BeFalse();

        time.Advance(TimeSpan.FromDays(1));
        (await job.NextRunAsync()).Occurrence.Should().Be(Nov23Noon);
        await time.WaitForDelayAsync();
        (await LastCompletedAsync()).Should().Be(Nov23Noon);
    }

    [Fact]
    public async Task FailedRun_IsRetriedWhenTheAppStartsAgain()
    {
        await _stateFile.WriteLastCompletedOccurrenceAsync(Nov21Noon);
        var firstTime = new ObservableTimeProvider(Nov22Noon.AddHours(-1));
        var first = await StartJobAsync(firstTime, _ => throw new InvalidOperationException("Database unreachable"));
        await firstTime.WaitForDelayAsync();
        firstTime.Advance(TimeSpan.FromHours(1));
        (await first.NextRunAsync()).Occurrence.Should().Be(Nov22Noon);
        await firstTime.WaitForDelayAsync();
        await first.StopAsync(CancellationToken.None);

        // The app goes to sleep and is woken up again half an hour later.
        var secondTime = new ObservableTimeProvider(Nov22Noon.AddMinutes(30));
        var second = await StartJobAsync(secondTime);

        (await second.NextRunAsync()).Occurrence.Should().Be(Nov22Noon);
        await secondTime.WaitForDelayAsync();
        (await LastCompletedAsync()).Should().Be(Nov22Noon);
    }

    [Fact]
    public async Task RunLastingPastTheNextOccurrence_RunsItRightAfterwards_NeitherSkippedNorDoubled()
    {
        await _stateFile.WriteLastCompletedOccurrenceAsync(Nov21Noon);
        var time = new ObservableTimeProvider(Nov23Noon.AddMinutes(-1)); // 11:59: the 22nd was missed
        var job = await StartJobAsync(time, occurrence =>
        {
            if (occurrence == Nov22Noon)
                time.Advance(TimeSpan.FromMinutes(5)); // the catch-up run takes until 12:04
            return Task.CompletedTask;
        });

        (await job.NextRunAsync()).Occurrence.Should().Be(Nov22Noon);
        (await job.NextRunAsync()).Should().Be(new Run(Nov23Noon, At: Nov23Noon.AddMinutes(4)));
        await time.WaitForDelayAsync();

        job.HasUnreadRun.Should().BeFalse();
        (await LastCompletedAsync()).Should().Be(Nov23Noon);
    }

    [Fact]
    public async Task FallingBehindBySeveralOccurrences_RunsOnlyTheLatestOne()
    {
        await _stateFile.WriteLastCompletedOccurrenceAsync(Nov21Noon);
        var time = new ObservableTimeProvider(Nov22Noon.AddHours(-1));
        var job = await StartJobAsync(time, occurrence =>
        {
            if (occurrence == Nov22Noon)
                time.Advance(TimeSpan.FromDays(3)); // e.g. the machine was suspended during the run
            return Task.CompletedTask;
        });
        await time.WaitForDelayAsync();

        time.Advance(TimeSpan.FromHours(1));
        (await job.NextRunAsync()).Occurrence.Should().Be(Nov22Noon);
        (await job.NextRunAsync()).Occurrence.Should().Be(Nov22Noon.AddDays(3));
        await time.WaitForDelayAsync();

        job.HasUnreadRun.Should().BeFalse();
    }

    [Fact]
    public async Task StopAsync_EndsTheWaitingLoop()
    {
        var time = new ObservableTimeProvider(Nov22Noon.AddHours(-1));
        var job = await StartJobAsync(time);
        await time.WaitForDelayAsync();

        await job.StopAsync(CancellationToken.None);

        job.ExecuteTask!.IsCompletedSuccessfully.Should().BeTrue();
    }

    private sealed record Run(DateTimeOffset Occurrence, DateTimeOffset At);

    /// <summary>A fake clock that signals each timer the loop creates, i.e. each time it starts waiting.</summary>
    private sealed class ObservableTimeProvider(DateTimeOffset now) : FakeTimeProvider(now)
    {
        private readonly SemaphoreSlim _timerCreated = new(0);

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            var timer = base.CreateTimer(callback, state, dueTime, period);
            _timerCreated.Release();
            return timer;
        }

        /// <summary>Waits until the loop waits for its next occurrence (all earlier work, incl. recording, is done).</summary>
        public async Task WaitForDelayAsync() =>
            (await _timerCreated.WaitAsync(Timeout)).Should().BeTrue("the job should wait for its next occurrence");
    }

    private sealed class TestJob(string cron, CronJobStateFile stateFile, TimeProvider time,
        Func<DateTimeOffset, Task> onRun)
        : CronJobService(cron, TimeZoneInfo.Utc, stateFile, time, NullLogger.Instance)
    {
        private readonly Channel<Run> _runs = Channel.CreateUnbounded<Run>();

        public bool HasUnreadRun => _runs.Reader.TryPeek(out _);

        public override async Task DoWorkAsync(DateTimeOffset occurrence, CancellationToken cancellationToken)
        {
            _runs.Writer.TryWrite(new Run(occurrence, Clock.GetUtcNow()));
            await onRun(occurrence);
        }

        public async Task<Run> NextRunAsync()
        {
            using var timeout = new CancellationTokenSource(Timeout);
            return await _runs.Reader.ReadAsync(timeout.Token);
        }
    }
}
