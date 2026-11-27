using EmailService.EmailServiceContracts;

namespace EmailService.Interfaces;

public interface IEmailPreparator
{
    /// <summary>
    /// Builds one "Celebration" e-mail per user for the persons whose birthday or name day is on <paramref name="today"/>,
    /// the day after or 14 days later. Returns an empty list when nobody celebrates.
    /// </summary>
    Task<IReadOnlyList<Message>> PrepareMessagesAsync(DateOnly today, CancellationToken cancellationToken = default);
}
