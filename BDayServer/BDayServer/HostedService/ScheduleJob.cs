using Interfaces.EmailService;
using Interfaces.Scheduler;

namespace BDayServer.HostedService;

public class ScheduleJob(
    IScheduleConfig<ScheduleJob> config,
    ILogger<ScheduleJob> logger,
    IServiceProvider serviceProvider)
    : CronJobService(config.CronExpression, config.TimeZoneInfo)
{
    private readonly IEmailSender _emailSender =
        serviceProvider.CreateScope().ServiceProvider.GetRequiredService<IEmailSender>();

    private readonly IEmailPreparator _emailPreparator =
        serviceProvider.CreateScope().ServiceProvider.GetRequiredService<IEmailPreparator>();

    //https://www.thecodebuzz.com/cannot-consume-scoped-service-from-singleton-ihostedservice/

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("ScheduleJob starts.");
        return base.StartAsync(cancellationToken);
    }

    public override async Task DoWork(CancellationToken cancellationToken)
    {
        logger.LogInformation($"{DateTime.Now:hh:mm:ss} ScheduleJob is working.");

        var messages = _emailPreparator.PrepareMessage();

        if (messages is not null)
            foreach (var message in messages)
                await _emailSender.SendEmailAsync(message);
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("ScheduleJob is stopping.");
        return base.StopAsync(cancellationToken);
    }
}