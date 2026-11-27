using Core.CronJobService;
using EmailService.Interfaces;

namespace BDayServer.HostedService;

/// <summary>
/// E-mails every user the birthdays and name days of their persons that are on the day of the occurrence, the day
/// after or 14 days later.
/// </summary>
public sealed class ScheduleJob(
    IScheduleConfig<ScheduleJob> config,
    IServiceScopeFactory scopeFactory,
    CronJobStateFile stateFile,
    TimeProvider timeProvider,
    ILogger<ScheduleJob> logger)
    : CronJobService(config.CronExpression, config.TimeZoneInfo, stateFile, timeProvider, logger)
{
    public override async Task DoWorkAsync(DateTimeOffset occurrence, CancellationToken cancellationToken)
    {
        // The day the occurrence belongs to, so a run caught up the next morning sends the reminders of the missed day
        // instead of repeating today's.
        var day = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(occurrence, TimeZone).DateTime);

        // A scope per run: the DbContext and the e-mail services are scoped and must not live as long as this singleton.
        await using var scope = scopeFactory.CreateAsyncScope();
        var preparator = scope.ServiceProvider.GetRequiredService<IEmailPreparator>();
        var sender = scope.ServiceProvider.GetRequiredService<IEmailSender>();

        var messages = await preparator.PrepareMessagesAsync(day, cancellationToken);

        var sent = 0;
        var failed = 0;
        for (var i = 0; i < messages.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await sender.SendEmailAsync(messages[i], cancellationToken);
                sent++;
            }
            catch (Exception ex) when (!(ex is OperationCanceledException && cancellationToken.IsCancellationRequested))
            {
                // One bad recipient must not stop the others. Addresses are personal data: only logged at Debug.
                failed++;
                Logger.LogWarning("Celebration e-mail {Number} of {Count} for {Day:yyyy-MM-dd} was not sent: {Error}",
                    i + 1, messages.Count, day, ex.GetType().Name);
                Logger.LogDebug(ex, "The celebration e-mail to {Recipients} was not sent",
                    string.Join(", ", messages[i].To.Select(to => to.Address)));
            }
        }

        Logger.Log(failed == 0 ? LogLevel.Information : LogLevel.Warning,
            "Celebration e-mails for {Day:yyyy-MM-dd}: {Sent} sent, {Failed} failed", day, sent, failed);

        // Nothing could be sent (SMTP server down, wrong credentials): the run failed as a whole and must not be recorded
        // as completed, so the next start of the app tries again.
        if (failed > 0 && sent == 0)
            throw new InvalidOperationException($"None of the {failed} celebration e-mail(s) could be sent.");
    }
}
