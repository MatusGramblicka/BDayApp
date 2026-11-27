namespace EmailService.EmailService;

/// <summary>Date arithmetic for yearly celebrations (birthdays, name days).</summary>
public static class CelebrationCalendar
{
    /// <summary>
    /// Days from <paramref name="today"/> until the next anniversary of <paramref name="date"/> (only its month and day
    /// count): 0 when it is today, 1 when it is tomorrow - also across the end of the year. 29 February is celebrated on
    /// 28 February in years that are not leap years.
    /// </summary>
    public static int DaysUntilNextAnniversary(DateOnly date, DateOnly today)
    {
        var next = AnniversaryIn(today.Year, date);
        if (next < today)
            next = AnniversaryIn(today.Year + 1, date);

        return next.DayNumber - today.DayNumber;
    }

    private static DateOnly AnniversaryIn(int year, DateOnly date) =>
        date is { Month: 2, Day: 29 } && !DateTime.IsLeapYear(year)
            ? new DateOnly(year, 2, 28)
            : new DateOnly(year, date.Month, date.Day);
}
