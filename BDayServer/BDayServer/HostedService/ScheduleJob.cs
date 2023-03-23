using AutoMapper;
using Contracts;
using EmailService;
using Entities;
using Entities.RequestFeatures;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Entities.DataTransferObjects.Person;

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

            var messages = await PrepareMessage();

            if (messages != null)
            { 
                foreach(var message in messages)
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

        private async Task<List<Message>> PrepareMessage()
        {
            var allUsersEmails = _userManager
                .Users
                .Select(s => s.Email)
                .ToArray();

            var personsFromDb = await _repository.Person.GetAllPersonsAsync(new PersonParameters { PageSize = 200 }, trackChanges: false);
            var personsDto = _mapper.Map<IEnumerable<PersonDto>>(personsFromDb);
                        
            var messageBirthDays = PrepareMessage(personsDto, HasCloseBirthDay, DayType.Birthday);
            var messageNameDays = PrepareMessage(personsDto, HasCloseNameDay, DayType.NameDay);

            var massages = new List<Message>();

            if (messageBirthDays.Count == 0 && messageNameDays.Count == 0)
                return null;
            else
            {
                var BirthAndName = new List<ReceipientMessage>();
                BirthAndName.AddRange(messageBirthDays);
                BirthAndName.AddRange(messageNameDays);

                var messageDaysByPersonCreators = BirthAndName.GroupBy(m => m.Receipient);
                foreach(var messageDaysByPersonCreator in messageDaysByPersonCreators)
                {
                    if(!allUsersEmails.Contains(messageDaysByPersonCreator.Key))
                    { 
                        continue; 
                    }    

                    var receipients = new string[] { messageDaysByPersonCreator.Key };
                    
                    var messageBirth = "";
                    var messageName = "";
                    foreach (var receipientMess in messageDaysByPersonCreator)
                    {
                        if (receipientMess.CelebrationType.Equals(DayType.Birthday))
                        {
                            messageBirth += receipientMess.Message; 
                        }
                        else
                        {
                            messageName += receipientMess.Message;
                        }
                    }

                    massages.Add(new Message(receipients, "Celebration", $"{messageBirth}{Environment.NewLine}{messageName}", null));
                }

                return massages;
                //return new Message(allUsersEmails, "Celebration", $"{messageBirthDays}{Environment.NewLine}{messageNameDays}", null);
            }                
        }

        private List<ReceipientMessage> PrepareMessage(IEnumerable<PersonDto> personsDto, Func<PersonDto, bool> hasCloseEvent, DayType dayType)
        {
            var personsDay = personsDto
                .Where(p => hasCloseEvent(p));
                
            var personsDayString = "";

            var methodName = hasCloseEvent.Method.Name;

            var receipientMessageList = new List<ReceipientMessage>();

            if (methodName.Contains("birth", StringComparison.InvariantCultureIgnoreCase))
            {
                foreach (var person in personsDay)
                {                    
                    receipientMessageList.Add(new ReceipientMessage 
                    { 
                        Message = $"{person.Name} {person.Surname} {person.DayOfBirth:dd/MM}\n", 
                        Receipient = person.PersonCreator,
                        CelebrationType = dayType
                    });
                }                    
            }
            else if (methodName.Contains("name", StringComparison.InvariantCultureIgnoreCase))
            {
                foreach (var person in personsDay)
                {                    
                    receipientMessageList.Add(new ReceipientMessage
                    {
                        Message = $"{person.Name} {person.Surname} {person.DayOfBirth:dd/MM}\n",
                        Receipient = person.PersonCreator,
                        CelebrationType = dayType
                    });
                }
            }

            _logger.LogInformation($"{DateTime.Now:hh:mm:ss} ScheduleJob found those people who have close day celebration {personsDayString}");

            if (receipientMessageList.All(r => r.Message.Length == 0))        
                return new List<ReceipientMessage>();
            else
                return receipientMessageList;
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
