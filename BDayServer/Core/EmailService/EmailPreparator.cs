using Contracts.EmailService;
using Interfaces.EmailService;
using Microsoft.Extensions.Logging;
using Repository;

namespace Core.EmailService;

public class EmailPreparator : IEmailPreparator
{
    private readonly ILogger<EmailPreparator> _logger;
    private readonly RepositoryContextScheduleJob _repositoryContextScheduleJob;

    public EmailPreparator(ILogger<EmailPreparator> logger, RepositoryContextScheduleJob repositoryContextScheduleJob)
    {
        _logger = logger;
        _repositoryContextScheduleJob = repositoryContextScheduleJob;
    }

    public List<Message>? PrepareMessage()
    {
        var personForEmailCreation = _repositoryContextScheduleJob.Persons.Select(p => new PersonForEmailCreation
        {
            CreatorEmail = p.User.Email,
            Name = p.Name,
            Surname = p.Surname,
            DayOfBirth = p.DayOfBirth,
            DayOfNameDay = p.DayOfNameDay
        });

        var messageBirthAndNameDays = PrepareMessagesBirthAndNameDay(personForEmailCreation);

        var massages = new List<Message>();

        if (messageBirthAndNameDays.Count == 0)
            return null;

        var messageDaysByPersonCreators = messageBirthAndNameDays.GroupBy(m => m.Recipient);
        foreach (var messageDaysByPersonCreator in messageDaysByPersonCreators)
        {
            var recipients = new[] {messageDaysByPersonCreator.Key};

            var messageBirth = "";
            var messageName = "";
            foreach (var recipientsMess in messageDaysByPersonCreator)
            {
                if (recipientsMess.CelebrationType.Equals(DayType.Birthday))
                    messageBirth += recipientsMess.Message;
                else
                    messageName += recipientsMess.Message;
            }

            massages.Add(new Message(recipients, "Celebration",
                $"Birthday:{Environment.NewLine}{messageBirth}{Environment.NewLine}" +
                $"Nameday:{Environment.NewLine}{messageName}", null));
        }

        return massages;
    }

    private List<RecipientMessage> PrepareMessagesBirthAndNameDay(
        IEnumerable<PersonForEmailCreation> personForEmailCreation)
    {
        var birthDayPersons = personForEmailCreation
            .Where(p => HasCloseDay(p.DayOfBirth));

        var recipientsMessageListBirthDay = birthDayPersons.Select(birthDayPerson => new RecipientMessage
        {
            Message = $"{birthDayPerson.Name} {birthDayPerson.Surname} {birthDayPerson.DayOfBirth:dd/MM}\n",
            Recipient = birthDayPerson.CreatorEmail,
            CelebrationType = DayType.Birthday
        });

        var nameDayPersons = personForEmailCreation
            .Where(p => HasCloseDay(p.DayOfNameDay));

        var recipientsMessageListNameDay = nameDayPersons.Select(nameDayPerson => new RecipientMessage
        {
            Message = $"{nameDayPerson.Name} {nameDayPerson.Surname} {nameDayPerson.DayOfNameDay:dd/MM}\n",
            Recipient = nameDayPerson.CreatorEmail,
            CelebrationType = DayType.NameDay
        });

        var recipientsMessageList = new List<RecipientMessage>();
        recipientsMessageList.AddRange(recipientsMessageListBirthDay);
        recipientsMessageList.AddRange(recipientsMessageListNameDay);

        return recipientsMessageList.All(r => r.Message.Length == 0)
            ? new List<RecipientMessage>()
            : recipientsMessageList;
    }

    private bool HasCloseDay(DateTime dateTime)
    {
        _logger.LogInformation($"{DateTime.Now:hh:mm:ss} ScheduleJob is searching " +
                               "for people with close birthday.");
        var timeNow = DateTime.Today;

        var age = timeNow.Year - dateTime.Year;
        var numOfDays = (dateTime - timeNow.AddYears(-age)).Days;

        return numOfDays is 14 or 1 or 0;
    }
}