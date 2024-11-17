using Contracts.EmailService.Models;
using EmailService.Extensions;
using Interfaces.EmailService;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Core.EmailService;

public class EmailSender : IEmailSender
{
    private const string AuthenticationMechanisms = "XOAUTH2";

    private readonly ILogger<EmailSender> _logger;

    private readonly EmailConfiguration _emailConfig;

    public EmailSender(IOptions<EmailConfiguration> emailConfig, ILogger<EmailSender> logger)
    {
        _emailConfig = emailConfig.Value;
        _logger = logger;
    }

    public void SendEmail(Message message)
    {
        ArgumentNullException.ThrowIfNull(message);

        var emailMessage = message.CreateEmailMessage(_emailConfig.From);

        Send(emailMessage);
    }

    public async Task SendEmailAsync(Message message)
    {
        ArgumentNullException.ThrowIfNull(message);

        var mailMessage = message.CreateEmailMessage(_emailConfig.From);

        await SendAsync(mailMessage);
    }

    private void Send(MimeMessage mailMessage)
    {
        using var client = new SmtpClient();
        try
        {
            client.Connect(_emailConfig.SmtpServer, _emailConfig.Port, true);
            client.AuthenticationMechanisms.Remove(AuthenticationMechanisms);
            client.Authenticate(_emailConfig.UserName, _emailConfig.Password);

            client.Send(mailMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Sending EmailConfiguration Exception: {ex}");
            throw;
        }
        finally
        {
            client.Disconnect(true);
            client.Dispose();
        }
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
            _logger.LogError($"Sending EmailConfiguration Exception: {ex}");
            throw;
        }
        finally
        {
            await client.DisconnectAsync(true);
            client.Dispose();
        }
    }
}