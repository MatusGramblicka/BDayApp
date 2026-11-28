using EmailService.EmailServiceContracts;

namespace EmailService.Interfaces;

public interface IEmailSender
{
    Task SendEmailAsync(Message message);

    /// <summary>
    /// Sends <paramref name="message"/>; <paramref name="cancellationToken"/> cancels connecting, authenticating and sending.
    /// (An overload rather than an optional parameter, so existing one-argument callers and their mocks keep working.)
    /// </summary>
    Task SendEmailAsync(Message message, CancellationToken cancellationToken);
}
