using BDayClient.Features;

namespace BDayClient.UnitTests.Features;

public class DayMonthTests
{
    private static DateOnly D(int year, int month, int day) => new(year, month, day);

    [Theory]
    [InlineData(2026, 5, 10, 2000, 5, 10, 0)]   // today
    [InlineData(2026, 5, 10, 1990, 5, 11, 1)]   // tomorrow; the year of the date does not matter
    [InlineData(2026, 5, 10, 2000, 5, 9, 364)]  // yesterday: next year
    [InlineData(2026, 12, 20, 2000, 1, 5, 16)]  // review finding: across the new year (was -349)
    [InlineData(2026, 12, 31, 2000, 1, 1, 1)]
    [InlineData(2026, 1, 1, 2000, 12, 31, 364)]
    public void DaysUntilNextOccurrence_CountsToTheNextDayAndMonth(int ty, int tm, int td, int y, int m, int d, int expected)
    {
        DayMonth.DaysUntilNextOccurrence(D(y, m, d), D(ty, tm, td)).Should().Be(expected);
    }

    [Fact]
    public void LeapDay_InNonLeapYear_FallsOn28February()
    {
        var leapDay = D(2000, 2, 29);

        DayMonth.NextOccurrence(leapDay, D(2026, 2, 1)).Should().Be(D(2026, 2, 28));
        DayMonth.DaysUntilNextOccurrence(leapDay, D(2026, 2, 28)).Should().Be(0);
    }

    [Fact]
    public void LeapDay_InLeapYear_Is29February()
    {
        var leapDay = D(2000, 2, 29);

        DayMonth.NextOccurrence(leapDay, D(2028, 2, 1)).Should().Be(D(2028, 2, 29));
        DayMonth.NextOccurrence(leapDay, D(2027, 3, 1)).Should().Be(D(2028, 2, 29));
    }

    [Fact]
    public void LeapDay_After28FebruaryOfNonLeapYear_IsNextYear()
    {
        // On 1 March 2026 the 2026 occurrence (28 February) has passed; 2027 is not a leap year either.
        DayMonth.NextOccurrence(D(2000, 2, 29), D(2026, 3, 1)).Should().Be(D(2027, 2, 28));
    }

    [Fact]
    public void InStorageYear_KeepsDayAndMonth()
    {
        DayMonth.InStorageYear(D(2028, 2, 29)).Should().Be(D(2000, 2, 29));
        DayMonth.InStorageYear(D(2026, 11, 22)).Should().Be(D(2000, 11, 22));
    }
}
