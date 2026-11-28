using System.Net.Sockets;
using EmailService.EmailService;
using EmailService.EmailServiceContracts;
using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MimeKit;

namespace BDayServer.UnitTests.EmailService;

/// <summary>Runs <see cref="EmailSender"/> against a mocked SMTP client - no network, no real SMTP server.</summary>
public class EmailSenderTests
{
    private readonly Mock<ISmtpClient> _client = new();
    private readonly HashSet<string> _authenticationMechanisms = ["XOAUTH2", "PLAIN"];
    private readonly Message _message = new(["jane@test.com"], "Subject", "Body", null);

    public EmailSenderTests()
    {
        _client.SetupGet(c => c.AuthenticationMechanisms).Returns(_authenticationMechanisms);
    }

    private EmailSender CreateSut() => new TestableEmailSender(_client.Object);

    private void SetupSendThrows(Exception exception)
    {
        _client.Setup(c => c.SendAsync(It.IsAny<ISendRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);
        _client.Setup(c => c.SendAsync(It.IsAny<FormatOptions>(), It.IsAny<ISendRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);
    }

    [Fact]
    public async Task SendEmailAsync_Success_PassesTheTokenAndDisconnects()
    {
        _client.SetupGet(c => c.IsConnected).Returns(true);
        using var cancellation = new CancellationTokenSource();

        await CreateSut().SendEmailAsync(_message, cancellation.Token);

        _client.Verify(c => c.ConnectAsync("smtp.test", 465, true, cancellation.Token), Times.Once);
        _client.Verify(c => c.AuthenticateAsync("sender", "secret", cancellation.Token), Times.Once);
        _client.Verify(c => c.DisconnectAsync(true, cancellation.Token), Times.Once);
        _client.Verify(c => c.Dispose(), Times.Once);
        _authenticationMechanisms.Should().NotContain("XOAUTH2");
    }

    [Fact]
    public async Task SendEmailAsync_ConnectFails_DoesNotDisconnectAndRethrowsTheConnectError()
    {
        var connectError = new SocketException((int)SocketError.HostNotFound);
        _client.Setup(c => c.ConnectAsync("smtp.test", 465, true, It.IsAny<CancellationToken>()))
            .ThrowsAsync(connectError);
        _client.SetupGet(c => c.IsConnected).Returns(false);

        var act = () => CreateSut().SendEmailAsync(_message, CancellationToken.None);

        (await act.Should().ThrowAsync<SocketException>()).Which.Should().BeSameAs(connectError);
        _client.Verify(c => c.DisconnectAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
        _client.Verify(c => c.Dispose(), Times.Once);
    }

    [Fact]
    public async Task SendEmailAsync_SendFails_DisconnectsAndRethrowsTheSendError_EvenIfDisconnectFailsToo()
    {
        _client.SetupGet(c => c.IsConnected).Returns(true);
        var sendError = new SmtpCommandException(SmtpErrorCode.RecipientNotAccepted, SmtpStatusCode.MailboxUnavailable,
            new MailboxAddress("", "jane@test.com"), "5.1.1 <jane@test.com>: Recipient address rejected");
        SetupSendThrows(sendError);
        _client.Setup(c => c.DisconnectAsync(true, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("Connection reset"));

        var act = () => CreateSut().SendEmailAsync(_message, CancellationToken.None);

        (await act.Should().ThrowAsync<SmtpCommandException>()).Which.Should().BeSameAs(sendError);
        _client.Verify(c => c.DisconnectAsync(true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendEmailAsync_AuthenticationFails_DisconnectsAndRethrowsTheAuthenticationError()
    {
        _client.SetupGet(c => c.IsConnected).Returns(true);
        var authenticationError = new AuthenticationException("535: 5.7.8 Username and Password not accepted");
        _client.Setup(c => c.AuthenticateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(authenticationError);

        var act = () => CreateSut().SendEmailAsync(_message, CancellationToken.None);

        (await act.Should().ThrowAsync<AuthenticationException>()).Which.Should().BeSameAs(authenticationError);
        _client.Verify(c => c.DisconnectAsync(true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendEmailAsync_DisconnectFailsAfterTheMessageWasSent_DoesNotFail()
    {
        _client.SetupGet(c => c.IsConnected).Returns(true);
        _client.Setup(c => c.DisconnectAsync(true, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("Connection reset"));

        var act = () => CreateSut().SendEmailAsync(_message, CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SendEmailAsync_WithoutToken_SendsWithoutCancellation()
    {
        _client.SetupGet(c => c.IsConnected).Returns(true);

#pragma warning disable xUnit1051 // This test is about the overload without a token.
        await CreateSut().SendEmailAsync(_message);
#pragma warning restore xUnit1051

        _client.Verify(c => c.ConnectAsync("smtp.test", 465, true, CancellationToken.None), Times.Once);
    }

    private sealed class TestableEmailSender(ISmtpClient client) : EmailSender(
        Options.Create(new EmailConfiguration
        {
            From = "app@test.com",
            SmtpServer = "smtp.test",
            Port = 465,
            UserName = "sender",
            Password = "secret"
        }),
        NullLogger<EmailSender>.Instance)
    {
        protected override ISmtpClient CreateSmtpClient() => client;
    }
}
