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

            if (message != null)
                await _emailSender.SendEmailAsync(message);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("ScheduleJob is stopping.");
            return base.StopAsync(cancellationToken);
        }

        private async Task<Message> PrepareMessage()
        {
            var allUsersEmails = _userManager
                .Users
                .Select(s => s.Email)
                .ToArray();

            var personsFromDb = await _repository.Person.GetAllPersonsAsync(new PersonParameters { PageSize = 50 }, trackChanges: false);
            var personsDto = _mapper.Map<IEnumerable<PersonDto>>(personsFromDb);

            var messageBirthDays = PrepareMessage(personsDto, HasCloseBirthDay, "- birthday:");

            var messageNameDays = PrepareMessage(personsDto, HasCloseNameDay, "- nameday:");

            if (messageBirthDays.Length == 0 && messageNameDays.Length == 0)
                return null;
            else
                return new Message(allUsersEmails, "Celebration", $"{messageBirthDays}{Environment.NewLine}{messageNameDays}", null);
        }

        private string PrepareMessage(IEnumerable<PersonDto> personsDto, Func<PersonDto, bool> hasCloseEvent, string message)
        {
            var personsNameDay = personsDto
                .Where(p => hasCloseEvent(p))
                .ToList();

            var personsNameDayString = "";

            var methodName = hasCloseEvent.Method.Name;

            if (methodName.Contains("birth", StringComparison.InvariantCultureIgnoreCase))
            {
                foreach (var person in personsNameDay)
                    personsNameDayString += $"{person.Name} {person.Surname} {person.DayOfBirth:dd/MM/yyyy}\n";
            }
            else if (methodName.Contains("name", StringComparison.InvariantCultureIgnoreCase))
            {
                foreach (var person in personsNameDay)
                    personsNameDayString += $"{person.Name} {person.Surname} {person.DayOfNameDay:dd/MM/yyyy} \n";
            }

            _logger.LogInformation($"{DateTime.Now:hh:mm:ss} ScheduleJob found these people who have close nameday celebration {personsNameDayString}");

            if (personsNameDayString.Length == 0)
                return personsNameDayString;
            else
                return $"{message}\n{personsNameDayString}";
        }

        private bool HasCloseBirthDay(PersonDto person)
        {
            _logger.LogInformation($"{DateTime.Now:hh:mm:ss} ScheduleJob is searching for people with close birthday.");
            var timeNow = DateTime.Today;

            var age = timeNow.Year - person.DayOfBirth.Year;
            var numOfDays = (person.DayOfBirth - timeNow.AddYears(-age)).Days;

            return numOfDays == 14 || numOfDays == 1 || numOfDays == 0;
        }

        private bool HasCloseNameDay(PersonDto person)
        {
            _logger.LogInformation($"{DateTime.Now:hh:mm:ss} ScheduleJob is searching for people with close nameday.");
            var timeNow = DateTime.Today;

            var ageNameDay = timeNow.Year - person.DayOfNameDay.Year;
            var numOfDaysNameDay = (person.DayOfNameDay - timeNow.AddYears(-ageNameDay)).Days;

            return numOfDaysNameDay == 14 || numOfDaysNameDay == 1 || numOfDaysNameDay == 0;
        }
    }
}
