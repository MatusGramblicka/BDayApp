using EmailService.EmailServiceContracts;

namespace EmailService.Interfaces;

public interface IEmailSender
{
    Task SendEmailAsync(Message message);
}