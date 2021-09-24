using AutoMapper;
using Contracts;
using EmailService;
using Entities;
using Entities.DataTransferObjects;
using Entities.RequestFeatures;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BDayServer.HostedService
{
    public class ScheduleJob : CronJobService
    {
        private readonly ILogger<ScheduleJob> _logger;
        private readonly UserManager<User> _userManager;
        private readonly IRepositoryManager _repository;
        private readonly IMapper _mapper;
        private readonly IEmailSender _emailSender;

        public ScheduleJob(IScheduleConfig<ScheduleJob> config, ILogger<ScheduleJob> logger,
            IMapper mapper, IServiceProvider serviceProvider)
            : base(config.CronExpression, config.TimeZoneInfo)
        {
            _logger = logger;
            //https://www.thecodebuzz.com/cannot-consume-scoped-service-from-singleton-ihostedservice/
            _userManager = serviceProvider.CreateScope().ServiceProvider.GetRequiredService<UserManager<User>>();
            _repository = serviceProvider.CreateScope().ServiceProvider.GetRequiredService<IRepositoryManager>();
            _mapper = mapper;
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
           
            var message = await PrepareMessage();
            await _emailSender.SendEmailAsync(message);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("ScheduleJob is stopping.");
            return base.StopAsync(cancellationToken);
        }

        private static bool HasCloseCelebration(PersonDto person)
        {
            var timeNow = DateTime.Today;

            var age = timeNow.Year - person.DayOfBirth.Year;
            int numOfDays = (person.DayOfBirth - timeNow.AddYears(-age)).Days;

            var ageNameDay = timeNow.Year - person.DayOfNameDay.Year;
            int numOfDaysNameDay = (person.DayOfNameDay - timeNow.AddYears(-ageNameDay)).Days; ;

            bool result = ((numOfDays < 14) && (numOfDays >= 0)) || ((numOfDaysNameDay < 14) && (numOfDaysNameDay >= 0));
            return result;
        }

        private async Task<Message> PrepareMessage()
        {
            var allUsersEmails = _userManager
                .Users
                .Select(s => s.Email)
                .ToArray();

            var personsFromDB = await _repository.Person.GetAllPersonsAsync(new PersonParameters(), trackChanges: false);
            var personsDto = _mapper.Map<IEnumerable<PersonDto>>(personsFromDB);

            var PersonsSurname = personsDto
                .Where(p => HasCloseCelebration(p))
                .Select(s => s.Surname)
                .ToList();

            var surnames = "";

            foreach (var surname in PersonsSurname)
                surnames += surname + ", ";

            surnames = surnames.Remove(surnames.Length - 2) + ".";

            return new Message(allUsersEmails, "Celebration is close",
                $"People with event in few days: {surnames}", null);
        }
    }
}
