using Contracts.EmailService;

namespace Interfaces.EmailService;

public interface IEmailSender
{
    Task SendEmailAsync(Message message);
}