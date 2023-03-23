using AutoMapper;
using Contracts;
using EmailService.Contracts;
using EmailService.Contracts.Models;
using Entities;
using Entities.DataTransferObjects.Person;
using Entities.RequestFeatures;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EmailService
{
    public class EmailPreparator : IEmailPreparator
    {
        private readonly ILogger<EmailPreparator> _logger;
        private readonly UserManager<User> _userManager;
        private readonly IRepositoryManager _repository;
        private readonly IMapper _mapper;


        public EmailPreparator(ILogger<EmailPreparator> logger, UserManager<User> userManager,
            IRepositoryManager repository, IMapper mapper)
        {

            _logger = logger;
            _userManager = userManager;
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<List<Message>> PrepareMessage()
        {
            var allUsersEmails = _userManager.Users.Select(s => s.Email).ToArray();

            var personsFromDb =
                await _repository.Person.GetAllPersonsAsync(new PersonParameters {PageSize = 200}, trackChanges: false);
            var personsDto = _mapper.Map<IEnumerable<PersonDto>>(personsFromDb);

            var messageBirthDays = PrepareMessage(personsDto, HasCloseBirthDay, DayType.Birthday);
            var messageNameDays = PrepareMessage(personsDto, HasCloseNameDay, DayType.NameDay);

            var massages = new List<Message>();

            if (messageBirthDays.Count == 0 && messageNameDays.Count == 0)
            {
                return null;
            }

            var birthAndName = new List<ReceipientMessage>();
            birthAndName.AddRange(messageBirthDays);
            birthAndName.AddRange(messageNameDays);

            var messageDaysByPersonCreators = birthAndName.GroupBy(m => m.Receipient);
            foreach (var messageDaysByPersonCreator in messageDaysByPersonCreators)
            {
                if (!allUsersEmails.Contains(messageDaysByPersonCreator.Key))
                {
                    continue;
                }

                var recipients = new[] {messageDaysByPersonCreator.Key};

                var messageBirth = "";
                var messageName = "";
                foreach (var recipientsMess in messageDaysByPersonCreator)
                {
                    if (recipientsMess.CelebrationType.Equals(DayType.Birthday))
                    {
                        messageBirth += recipientsMess.Message;
                    }
                    else
                    {
                        messageName += recipientsMess.Message;
                    }
                }

                massages.Add(new Message(recipients, "Celebration",
                    $"{messageBirth}{Environment.NewLine}{messageName}", null));
            }

            return massages;
        }

        private static List<ReceipientMessage> PrepareMessage(IEnumerable<PersonDto> personsDto,
            Func<PersonDto, bool> hasCloseEvent, DayType dayType)
        {
            var personsDay = personsDto
                .Where(hasCloseEvent);

            var methodName = hasCloseEvent.Method.Name;
            var recipientsMessageList = new List<ReceipientMessage>();

            if (methodName.Contains("birth", StringComparison.InvariantCultureIgnoreCase))
            {
                foreach (var person in personsDay)
                {
                    recipientsMessageList.Add(new ReceipientMessage
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
                    recipientsMessageList.Add(new ReceipientMessage
                    {
                        Message = $"{person.Name} {person.Surname} {person.DayOfBirth:dd/MM}\n",
                        Receipient = person.PersonCreator,
                        CelebrationType = dayType
                    });
                }
            }

            return recipientsMessageList.All(r => r.Message.Length == 0)
                ? new List<ReceipientMessage>()
                : recipientsMessageList;
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