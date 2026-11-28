using System.Diagnostics.CodeAnalysis;
using EmailService.EmailServiceContracts;
using EmailService.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MimeKit;
using Repository.Repositories;

namespace EmailService.EmailService;

public class EmailPreparator(ILogger<EmailPreparator> logger, PostgreDbRepositoryContext context)
    : IEmailPreparator
{
    // Without this, MimeKit also accepts a bare "name" (an address without a domain), which no SMTP server delivers.
    private static readonly ParserOptions AddressParserOptions = new() { AllowAddressesWithoutDomain = false };

    public async Task<IReadOnlyList<Message>> PrepareMessagesAsync(DateOnly today,
        CancellationToken cancellationToken = default)
    {
        var persons = await context.Persons
            .Select(p => new PersonForEmailCreation
            {
                CreatorEmail = p.User.Email,
                Name = p.Name,
                Surname = p.Surname,
                DayOfBirth = p.DayOfBirth,
                DayOfNameDay = p.DayOfNameDay
            })
            .ToListAsync(cancellationToken);

        var celebrations = FindCelebrations(persons, today);

        var messages = new List<Message>();
        var creatorsWithoutValidEmail = 0;

        foreach (var celebrationsOfCreator in celebrations.GroupBy(c => c.Recipient))
        {
            if (!TryGetAddress(celebrationsOfCreator.Key, out var address))
            {
                creatorsWithoutValidEmail++;
                logger.LogDebug("Skipping the celebrations of a user whose e-mail {Email} is not a valid address",
                    celebrationsOfCreator.Key);
                continue;
            }

            var messageBirth = "";
            var messageName = "";
            foreach (var celebration in celebrationsOfCreator)
            {
                if (celebration.CelebrationType == DayType.Birthday)
                    messageBirth += celebration.Message;
                else
                    messageName += celebration.Message;
            }

            messages.Add(new Message([address], "Celebration",
                $"Birthday:{Environment.NewLine}{messageBirth}{Environment.NewLine}" +
                $"Nameday:{Environment.NewLine}{messageName}", null));
        }

        if (creatorsWithoutValidEmail > 0)
            logger.LogWarning("{UserCount} user(s) with celebrations on {Date:yyyy-MM-dd} have no valid e-mail address " +
                              "and get no e-mail", creatorsWithoutValidEmail, today);

        logger.LogInformation("Prepared {MessageCount} celebration e-mail(s) for {Date:yyyy-MM-dd}: " +
                              "{CelebrationCount} celebration(s) among {PersonCount} person(s)",
            messages.Count, today, celebrations.Count, persons.Count);

        return messages;
    }

    private static List<RecipientMessage> FindCelebrations(List<PersonForEmailCreation> persons, DateOnly today)
    {
        var birthdays = persons
            .Where(p => p.DayOfBirth.HasValue && IsReminderDay(p.DayOfBirth.Value, today))
            .Select(p => new RecipientMessage
            {
                Message = FormatLine(p, p.DayOfBirth!.Value),
                Recipient = p.CreatorEmail,
                CelebrationType = DayType.Birthday
            });

        var nameDays = persons
            .Where(p => IsReminderDay(p.DayOfNameDay, today))
            .Select(p => new RecipientMessage
            {
                Message = FormatLine(p, p.DayOfNameDay),
                Recipient = p.CreatorEmail,
                CelebrationType = DayType.NameDay
            });

        return [.. birthdays, .. nameDays];
    }

    // Announced 14 days ahead, the day before and on the day itself.
    private static bool IsReminderDay(DateOnly date, DateOnly today) =>
        CelebrationCalendar.DaysUntilNextAnniversary(date, today) is 14 or 1 or 0;

    // "dd/MM" with an explicit culture: otherwise "/" becomes the date separator of the server's culture (e.g. "22.11").
    private static string FormatLine(PersonForEmailCreation person, DateOnly date) =>
        FormattableString.Invariant($"{person.Name} {person.Surname} {date:dd/MM}\n");

    private static bool TryGetAddress(string? email, [NotNullWhen(true)] out string? address)
    {
        address = null;
        if (string.IsNullOrWhiteSpace(email) ||
            !MailboxAddress.TryParse(AddressParserOptions, email, out var mailbox))
            return false;

        address = mailbox.Address;
        return true;
    }
}
