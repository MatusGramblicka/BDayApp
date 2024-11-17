using Contracts.EmailService.Models;

namespace Interfaces.EmailService;

public interface IEmailSender
{
    void SendEmail(Message message);
    Task SendEmailAsync(Message message);
}