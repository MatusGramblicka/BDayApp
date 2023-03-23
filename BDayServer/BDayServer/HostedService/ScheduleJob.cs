using EmailService;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using EmailService.Contracts;

namespace BDayServer.HostedService
{
    public class ScheduleJob : CronJobService
    {
        private readonly ILogger<ScheduleJob> _logger;
        private readonly IEmailSender _emailSender;
        private readonly IEmailPreparator _emailPreparator;

        public ScheduleJob(IScheduleConfig<ScheduleJob> config, ILogger<ScheduleJob> logger,
            IServiceProvider serviceProvider)
            : base(config.CronExpression, config.TimeZoneInfo)
        {
            _logger = logger;
            //https://www.thecodebuzz.com/cannot-consume-scoped-service-from-singleton-ihostedservice/
            _emailPreparator = serviceProvider.CreateScope().ServiceProvider.GetRequiredService<IEmailPreparator>();
            _emailSender = serviceProvider.CreateScope().ServiceProvider.GetRequiredService<IEmailSender>();
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("ScheduleJob starts.");
            return base.StartAsync(cancellationToken);
        }

        public override async Task DoWork(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{DateTime.Now:hh:mm:ss} ScheduleJob is working.");

            var messages = await _emailPreparator.PrepareMessage();
            _logger.LogInformation($"{DateTime.Now:hh:mm:ss} ScheduleJob gets messages count. {messages.Count}");
            if (messages != null)
            {
                foreach (var message in messages)
                {
                    await _emailSender.SendEmailAsync(message);
                }
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("ScheduleJob is stopping.");
            return base.StopAsync(cancellationToken);
        }
    }
}
