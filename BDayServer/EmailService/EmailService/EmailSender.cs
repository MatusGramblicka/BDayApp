using EmailService.EmailService.Extensions;
using EmailService.EmailServiceContracts;
using EmailService.Interfaces;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace EmailService.EmailService;

public class EmailSender(IOptions<EmailConfiguration> emailConfig, ILogger<EmailSender> logger)
    : IEmailSender
{
    private const string AuthenticationMechanisms = "XOAUTH2";

    private readonly EmailConfiguration _emailConfig = emailConfig.Value;
      
    public async Task SendEmailAsync(Message message)
    {
        ArgumentNullException.ThrowIfNull(message);

        var mailMessage = message.CreateEmailMessage(_emailConfig.From);

        await SendAsync(mailMessage);
    }

    private async Task SendAsync(MimeMessage mailMessage)
    {
        using var client = new SmtpClient();

        try
        {
            await client.ConnectAsync(_emailConfig.SmtpServer, _emailConfig.Port, true);
            client.AuthenticationMechanisms.Remove(AuthenticationMechanisms);
            await client.AuthenticateAsync(_emailConfig.UserName, _emailConfig.Password);

            await client.SendAsync(mailMessage);
        }
        catch (Exception ex)
        {
            logger.LogError($"Sending EmailConfiguration Exception: {ex}");
            throw;
        }
        finally
        {
            await client.DisconnectAsync(true);
        }
    }
}