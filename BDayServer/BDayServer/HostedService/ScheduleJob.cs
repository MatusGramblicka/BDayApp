using EmailService;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using EmailService.Contracts;
using Contracts;
using AutoMapper;
using Entities.DataTransferObjects.Person;
using Entities.RequestFeatures;
using System.Collections.Generic;
using System.Linq;
using Entities;

namespace BDayServer.HostedService
{
    public class ScheduleJob : CronJobService
    {
        private readonly ILogger<ScheduleJob> _logger;
        private readonly IEmailSender _emailSender;
        private readonly IEmailPreparator _emailPreparator;
        private readonly IRepositoryManager _repository;
        private readonly IMapper _mapper;
        private readonly RepositoryContextScheduleJob _repositoryContextScheduleJob;

        public ScheduleJob(IScheduleConfig<ScheduleJob> config, ILogger<ScheduleJob> logger,
            IMapper mapper, IServiceProvider serviceProvider)
            : base(config.CronExpression, config.TimeZoneInfo)
        {
            _logger = logger;
            //https://www.thecodebuzz.com/cannot-consume-scoped-service-from-singleton-ihostedservice/
            _emailPreparator = serviceProvider.CreateScope().ServiceProvider.GetRequiredService<IEmailPreparator>();
            _emailSender = serviceProvider.CreateScope().ServiceProvider.GetRequiredService<IEmailSender>();
            _repository = serviceProvider.CreateScope().ServiceProvider.GetRequiredService<IRepositoryManager>();
            _mapper = mapper;
            _repositoryContextScheduleJob= serviceProvider.CreateScope().ServiceProvider.GetService<RepositoryContextScheduleJob>();
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("ScheduleJob starts.");
            return base.StartAsync(cancellationToken);
        }

        public override async Task DoWork(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{DateTime.Now:hh:mm:ss} ScheduleJob is working.");

            //var personsFromDb =
            //    await _repository.Person.GetAllPersonsAsync(new PersonParameters { PageSize = 50 }, trackChanges: false);
            var personsFromDb = _repositoryContextScheduleJob.Persons.ToList();
            var personsDto = _mapper.Map<IEnumerable<PersonEmailDto>>(personsFromDb).ToList();

            _logger.LogInformation($"after mapper personsFromDb {personsFromDb.Count}");
            var messages = await _emailPreparator.PrepareMessage(personsDto);
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
