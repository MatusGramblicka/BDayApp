using BDayServer.HostedService;
using Core.CronJobService;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BDayServer.Extensions;

public static class ScheduledServiceExtensions
{
    /// <summary>Optional setting with the path of the scheduler's state file (see <see cref="CronJobStateFile"/>).</summary>
    public const string StateFileSetting = "Scheduler:StateFile";

    public static IServiceCollection AddCronJob<T>(this IServiceCollection services,
        Action<IScheduleConfig<T>> options) where T : CronJobService
    {
        if (options is null)
            throw new ArgumentNullException(nameof(options), "Please provide Schedule Configurations.");

        var config = new ScheduleConfig<T>();
        options.Invoke(config);
        if (string.IsNullOrWhiteSpace(config.CronExpression))
            throw new ArgumentNullException(nameof(ScheduleConfig<T>.CronExpression),
                "Empty Cron Expression is not allowed.");

        services.TryAddSingleton(TimeProvider.System);
        // One state file: it holds the last completed occurrence of the (single) scheduled job.
        services.TryAddSingleton(provider => new CronJobStateFile(
            CronJobStateFile.ResolvePath(
                provider.GetService<IConfiguration>()?[StateFileSetting],
                Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME"),
                Environment.GetEnvironmentVariable("HOME"),
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)),
            provider.GetRequiredService<ILogger<CronJobStateFile>>()));

        services.AddSingleton<IScheduleConfig<T>>(config);
        services.AddHostedService<T>();

        return services;
    }
}
