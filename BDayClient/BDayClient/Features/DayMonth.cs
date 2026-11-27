namespace BDayClient.Features;

/// <summary>Birthdays and namedays are a day and month; the year of the stored <see cref="DateOnly"/> is ignored.</summary>
public static class DayMonth
{
    /// <summary>The year the day/month pickers store dates in. A leap year, so that 29 February can be stored.</summary>
    public const int StorageYear = 2000;

    /// <summary>The given day and month in <see cref="StorageYear"/>.</summary>
    public static DateOnly InStorageYear(DateOnly date) => new(StorageYear, date.Month, date.Day);

    /// <summary>
    /// The next date, counting from <paramref name="today"/> (inclusive), that falls on the day and month of <paramref name="date"/>.
    /// 29 February falls on 28 February in years that are not leap years.
    /// </summary>
    public static DateOnly NextOccurrence(DateOnly date, DateOnly today)
    {
        var thisYear = OccurrenceIn(date, today.Year);
        return thisYear >= today ? thisYear : OccurrenceIn(date, today.Year + 1);
    }

    /// <summary>Days from <paramref name="today"/> until <see cref="NextOccurrence"/>: 0 when it is today, at most 365.</summary>
    public static int DaysUntilNextOccurrence(DateOnly date, DateOnly today) =>
        NextOccurrence(date, today).DayNumber - today.DayNumber;

    private static DateOnly OccurrenceIn(DateOnly date, int year) =>
        new(year, date.Month, Math.Min(date.Day, DateTime.DaysInMonth(year, date.Month)));
}
