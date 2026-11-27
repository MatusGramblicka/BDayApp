using EmailService.EmailService.Extensions;
using EmailService.EmailServiceContracts;
using EmailService.Interfaces;
using MailKit;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EmailService.EmailService;

public class EmailSender(IOptions<EmailConfiguration> emailConfig, ILogger<EmailSender> logger)
    : IEmailSender
{
    private const string AuthenticationMechanisms = "XOAUTH2";

    private readonly EmailConfiguration _emailConfig = emailConfig.Value;

    public Task SendEmailAsync(Message message) => SendEmailAsync(message, CancellationToken.None);

    public async Task SendEmailAsync(Message message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        var mailMessage = message.CreateEmailMessage(_emailConfig.From);

        using var client = CreateSmtpClient();
        var step = "connecting";
        try
        {
            await client.ConnectAsync(_emailConfig.SmtpServer, _emailConfig.Port, true, cancellationToken);
            client.AuthenticationMechanisms.Remove(AuthenticationMechanisms);

            step = "authenticating";
            await client.AuthenticateAsync(_emailConfig.UserName, _emailConfig.Password, cancellationToken);

            step = "sending";
            await client.SendAsync(mailMessage, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Exception messages can quote recipient addresses (e.g. a rejected RCPT TO), so the details are Debug only.
            logger.LogError("Sending an e-mail via {SmtpServer}:{Port} failed while {Step}: {Error}",
                _emailConfig.SmtpServer, _emailConfig.Port, step, DescribeError(ex));
            logger.LogDebug(ex, "E-mail sending failure details");
            throw;
        }
        finally
        {
            // Only when connected: after a failed connect, DisconnectAsync would throw and hide the original exception.
            if (client.IsConnected)
                await DisconnectAsync(client, cancellationToken);
        }
    }

    /// <summary>Creates the SMTP client used for one message.</summary>
    protected virtual ISmtpClient CreateSmtpClient() => new SmtpClient();

    private async Task DisconnectAsync(ISmtpClient client, CancellationToken cancellationToken)
    {
        try
        {
            await client.DisconnectAsync(true, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Shutting down: disposing the client closes the connection.
        }
        catch (Exception ex)
        {
            // A failed QUIT must neither hide the original error nor turn a message that was sent into a failure.
            logger.LogWarning("Disconnecting from {SmtpServer} failed: {Error}", _emailConfig.SmtpServer, DescribeError(ex));
            logger.LogDebug(ex, "SMTP disconnect failure details");
        }
    }

    private static string DescribeError(Exception exception) =>
        exception is SmtpCommandException smtp
            ? FormattableString.Invariant(
                $"{exception.GetType().Name} {smtp.ErrorCode}, SMTP status {(int)smtp.StatusCode} {smtp.StatusCode}")
            : exception.GetType().Name;
}
