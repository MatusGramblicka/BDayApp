namespace Core.CronJobService;

public interface IScheduleConfig<T>
{
    string CronExpression { get; set; }

    TimeZoneInfo TimeZoneInfo { get; set; }
}