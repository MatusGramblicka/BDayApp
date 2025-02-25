﻿using Cronos;

namespace BDayServer.HostedService;

//https://stackoverflow.com/questions/65987477/how-can-i-execute-a-method-every-year-using-backgroundservice-in-asp-net-core-ap
public class CronJobService : IHostedService, IDisposable
{
    private System.Timers.Timer _timer;

    private readonly CronExpression _expression;
    private readonly TimeZoneInfo _timeZoneInfo;

    private bool _alreadyDisposed;

    protected CronJobService(string cronExpression, TimeZoneInfo timeZoneInfo)
    {
        _expression = CronExpression.Parse(cronExpression);
        _timeZoneInfo = timeZoneInfo;
    }

    public virtual async Task StartAsync(CancellationToken cancellationToken)
    {
        await ScheduleJob(cancellationToken);
    }

    protected virtual async Task ScheduleJob(CancellationToken cancellationToken)
    {
        var next = _expression.GetNextOccurrence(DateTimeOffset.Now, _timeZoneInfo);
        if (next.HasValue)
        {
            var delay = next.Value - DateTimeOffset.Now;
            if (delay.TotalMilliseconds <= 0)
                await ScheduleJob(cancellationToken);
            
            _timer = new System.Timers.Timer(delay.TotalMilliseconds);
            _timer.Elapsed += async (sender, args) =>
            {
                _timer.Dispose(); // reset and dispose timer
                _timer = null;

                if (!cancellationToken.IsCancellationRequested)
                    await DoWork(cancellationToken);
                
                if (!cancellationToken.IsCancellationRequested)
                    await ScheduleJob(cancellationToken); // reschedule next

            };

            _timer.Start();
        }

        await Task.CompletedTask;
    }

    public virtual async Task DoWork(CancellationToken cancellationToken)
    {
        await Task.Delay(5000, cancellationToken);
    }

    public virtual async Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Stop();
        await Task.CompletedTask;
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (_alreadyDisposed)
            return;

        if (disposing)
        {
            _timer?.Dispose();
            _alreadyDisposed = true;
        }
    }

    ~CronJobService()
    {
        Dispose(disposing: false);
    }
}