using BDayServer.Extensions;
using BDayServer.HostedService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Time.Testing;

namespace BDayServer.UnitTests.HostedService;

public class ScheduledServiceExtensionsTests
{
    private static void AddScheduleJob(IServiceCollection services) =>
        services.AddCronJob<ScheduleJob>(c =>
        {
            c.TimeZoneInfo = TimeZoneInfo.Utc;
            c.CronExpression = "0 12 * * *";
        });

    [Fact]
    public void AddCronJob_RegistersTheJobWithTheSystemClock()
    {
        var services = new ServiceCollection().AddLogging();

        AddScheduleJob(services);

        using var provider = services.BuildServiceProvider();
        provider.GetServices<IHostedService>().Should().ContainSingle().Which.Should().BeOfType<ScheduleJob>();
        provider.GetRequiredService<TimeProvider>().Should().BeSameAs(TimeProvider.System);
    }

    [Fact]
    public void AddCronJob_KeepsATimeProviderThatIsAlreadyRegistered()
    {
        var clock = new FakeTimeProvider();
        var services = new ServiceCollection().AddLogging().AddSingleton<TimeProvider>(clock);

        AddScheduleJob(services);

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<TimeProvider>().Should().BeSameAs(clock);
    }

    [Fact]
    public void AddCronJob_StateFileSetting_DecidesWhereRunsAreRecorded()
    {
        var path = Path.Combine(Path.GetTempPath(), "BDayServer.UnitTests", "custom-last-run.txt");
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { [ScheduledServiceExtensions.StateFileSetting] = path })
            .Build();
        var services = new ServiceCollection().AddLogging().AddSingleton<IConfiguration>(configuration);

        AddScheduleJob(services);

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<CronJobStateFile>().FilePath.Should().Be(path);
    }

    [Fact]
    public void AddCronJob_EmptyCronExpression_Throws()
    {
        var act = () => new ServiceCollection().AddCronJob<ScheduleJob>(c => c.CronExpression = " ");

        act.Should().Throw<ArgumentNullException>();
    }
}
