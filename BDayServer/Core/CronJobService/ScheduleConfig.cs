using Interfaces.Scheduler;

namespace Core.CronJobService;

public class ScheduleConfig<T> : IScheduleConfig<T>
{
    public string CronExpression { get; set; } = string.Empty;

    public TimeZoneInfo TimeZoneInfo { get; set; }
}