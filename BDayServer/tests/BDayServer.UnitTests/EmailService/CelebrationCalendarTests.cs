using System.Globalization;
using EmailService.EmailService;

namespace BDayServer.UnitTests.EmailService;

public class CelebrationCalendarTests
{
    [Theory]
    // same day
    [InlineData("1990-06-15", "2026-06-15", 0)]
    [InlineData("1990-12-31", "2026-12-31", 0)]
    // across the end of the year
    [InlineData("1990-01-01", "2026-12-31", 1)]
    [InlineData("1990-01-14", "2026-12-31", 14)]
    [InlineData("1990-01-01", "2026-12-18", 14)]
    // just passed: next year
    [InlineData("1990-06-14", "2026-06-15", 364)]
    // 29 February: on 28 February in non-leap years, on 29 February in leap years
    [InlineData("1992-02-29", "2027-02-28", 0)]
    [InlineData("1992-02-29", "2027-02-27", 1)]
    [InlineData("1992-02-29", "2027-02-14", 14)]
    [InlineData("1992-02-29", "2028-02-28", 1)]
    [InlineData("1992-02-29", "2028-02-29", 0)]
    [InlineData("1992-02-29", "2027-03-01", 365)]
    // 1 March right after a leap day, and across a leap day
    [InlineData("1990-03-01", "2027-02-28", 1)]
    [InlineData("1990-03-01", "2028-02-29", 1)]
    [InlineData("1990-03-01", "2028-02-16", 14)]
    public void DaysUntilNextAnniversary(string date, string today, int expectedDays)
    {
        CelebrationCalendar.DaysUntilNextAnniversary(
                DateOnly.Parse(date, CultureInfo.InvariantCulture), DateOnly.Parse(today, CultureInfo.InvariantCulture))
            .Should().Be(expectedDays);
    }
}
