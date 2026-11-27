using BDayServer.HostedService;
using Core.CronJobService;
using EmailService.EmailServiceContracts;
using EmailService.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace BDayServer.UnitTests.HostedService;

public class ScheduleJobTests
{
    private static readonly DateTimeOffset Occurrence = new(2026, 11, 22, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Day = new(2026, 11, 22);

    private readonly Mock<IEmailPreparator> _preparator = new();
    private readonly Mock<IEmailSender> _sender = new();
    private readonly TestLogger<ScheduleJob> _logger = new();

    private static Message NewMessage(string recipient) => new([recipient], "Celebration", "Birthday: ...", null);

    private ScheduleJob CreateSut(Action<IServiceCollection>? registerServices = null)
    {
        var services = new ServiceCollection();
        if (registerServices is null)
        {
            services.AddScoped(_ => _preparator.Object);
            services.AddScoped(_ => _sender.Object);
        }
        else
        {
            registerServices(services);
        }

        return new ScheduleJob(
            new ScheduleConfig<ScheduleJob> { CronExpression = "0 12 * * *", TimeZoneInfo = TimeZoneInfo.Utc },
            services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>(),
            // Not touched by DoWorkAsync; recording runs is the scheduler's job.
            new CronJobStateFile(Path.Combine(Path.GetTempPath(), "unused", "last-run.txt"),
                NullLogger<CronJobStateFile>.Instance),
            TimeProvider.System,
            _logger);
    }

    private void SetupMessages(params Message[] messages) =>
        _preparator.Setup(p => p.PrepareMessagesAsync(Day, It.IsAny<CancellationToken>())).ReturnsAsync(messages);

    [Fact]
    public async Task DoWork_OneRecipientFails_TheOthersAreStillSent()
    {
        var first = NewMessage("first@test.com");
        var broken = NewMessage("broken@test.com");
        var third = NewMessage("third@test.com");
        SetupMessages(first, broken, third);
        _sender.Setup(s => s.SendEmailAsync(broken, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Mailbox broken@test.com unavailable"));

        await CreateSut().DoWorkAsync(Occurrence, CancellationToken.None);

        _sender.Verify(s => s.SendEmailAsync(first, It.IsAny<CancellationToken>()), Times.Once);
        _sender.Verify(s => s.SendEmailAsync(broken, It.IsAny<CancellationToken>()), Times.Once);
        _sender.Verify(s => s.SendEmailAsync(third, It.IsAny<CancellationToken>()), Times.Once);
        _logger.Entries.Should().Contain(e => e.Level == LogLevel.Warning && e.Message.Contains("2 sent, 1 failed"));
    }

    [Fact]
    public async Task DoWork_LogsNoPersonalDataAboveDebug()
    {
        var broken = NewMessage("broken@test.com");
        SetupMessages(NewMessage("first@test.com"), broken);
        _sender.Setup(s => s.SendEmailAsync(broken, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Mailbox broken@test.com unavailable"));

        await CreateSut().DoWorkAsync(Occurrence, CancellationToken.None);

        var aboveDebug = _logger.Entries.Where(e => e.Level > LogLevel.Debug).ToList();
        aboveDebug.Should().NotBeEmpty();
        aboveDebug.Should().AllSatisfy(e =>
        {
            e.Message.Should().NotContain("@test.com");
            e.Exception.Should().BeNull("exception messages can quote addresses");
        });
        _logger.Entries.Should().Contain(e => e.Level == LogLevel.Debug && e.Message.Contains("broken@test.com"));
    }

    [Fact]
    public async Task DoWork_AllMessagesFail_FailsTheRunSoItIsRetried()
    {
        SetupMessages(NewMessage("first@test.com"), NewMessage("second@test.com"));
        _sender.Setup(s => s.SendEmailAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("SMTP server unreachable"));

        var act = () => CreateSut().DoWorkAsync(Occurrence, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task DoWork_PreparingFails_FailsTheRun()
    {
        _preparator.Setup(p => p.PrepareMessagesAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database unreachable"));

        var act = () => CreateSut().DoWorkAsync(Occurrence, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Database unreachable");
        _sender.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DoWork_NoCelebrations_SendsNothing()
    {
        SetupMessages();

        await CreateSut().DoWorkAsync(Occurrence, CancellationToken.None);

        _sender.VerifyNoOtherCalls();
        _logger.Entries.Should().Contain(e => e.Level == LogLevel.Information && e.Message.Contains("0 sent, 0 failed"));
    }

    [Fact]
    public async Task DoWork_PreparesTheMessagesForTheDayOfTheOccurrence()
    {
        SetupMessages();
        using var cancellation = new CancellationTokenSource();

        // A run caught up the next morning still belongs to the 22nd.
        await CreateSut().DoWorkAsync(Occurrence, cancellation.Token);

        _preparator.Verify(p => p.PrepareMessagesAsync(Day, cancellation.Token), Times.Once);
    }

    [Fact]
    public async Task DoWork_Cancelled_StopsSendingAndFails()
    {
        var first = NewMessage("first@test.com");
        var second = NewMessage("second@test.com");
        SetupMessages(first, second);
        using var cancellation = new CancellationTokenSource();
        _sender.Setup(s => s.SendEmailAsync(first, It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                cancellation.Cancel(); // the app shuts down during the first message
                return Task.FromCanceled(cancellation.Token);
            });

        var act = () => CreateSut().DoWorkAsync(Occurrence, cancellation.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        _sender.Verify(s => s.SendEmailAsync(second, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DoWork_UsesNewScopedServicesForEachRun_AndDisposesThemAfterwards()
    {
        var preparators = new List<DisposablePreparator>();
        var sut = CreateSut(services =>
        {
            services.AddScoped<IEmailPreparator>(_ =>
            {
                var preparator = new DisposablePreparator();
                preparators.Add(preparator);
                return preparator;
            });
            services.AddScoped(_ => _sender.Object);
        });

        await sut.DoWorkAsync(Occurrence, CancellationToken.None);
        await sut.DoWorkAsync(Occurrence.AddDays(1), CancellationToken.None);

        preparators.Should().HaveCount(2);
        preparators.Should().OnlyContain(p => p.Disposed);
    }

    private sealed class DisposablePreparator : IEmailPreparator, IDisposable
    {
        public bool Disposed { get; private set; }

        public Task<IReadOnlyList<Message>> PrepareMessagesAsync(DateOnly today,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Message>>([]);

        public void Dispose() => Disposed = true;
    }
}
