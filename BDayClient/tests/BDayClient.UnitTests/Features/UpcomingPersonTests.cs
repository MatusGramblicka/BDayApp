using BDayClient.Features;
using Contracts.DataTransferObjects.Person;

namespace BDayClient.UnitTests.Features;

public class UpcomingPersonTests
{
    private static readonly DateOnly Today = new(2026, 12, 20);

    private static PersonDto Person(string name, DateOnly nameDay, DateOnly? birthday = null) =>
        new() { Id = Guid.NewGuid(), Name = name, DayOfNameDay = nameDay, DayOfBirth = birthday };

    [Fact]
    public void Within_KeepsOnlyTheNextDays_SoonestFirst()
    {
        var persons = new[]
        {
            Person("Late", nameDay: new DateOnly(2000, 1, 15)),                                  // 26 days
            Person("Far", nameDay: new DateOnly(2000, 6, 1)),                                    // outside
            Person("Soon", nameDay: new DateOnly(2000, 6, 1), birthday: new DateOnly(1990, 12, 22)), // birthday in 2 days
            Person("Today", nameDay: new DateOnly(2000, 12, 20)),
            Person("NewYear", nameDay: new DateOnly(2000, 1, 5))                                 // 16 days, across the year end
        };

        var upcoming = UpcomingPerson.Within(persons, Today, days: 30);

        upcoming.Select(u => u.Person.Name).Should().Equal("Today", "Soon", "NewYear", "Late");
        upcoming.Select(u => u.DaysUntil).Should().Equal(0, 2, 16, 26);
    }

    [Fact]
    public void Within_Day30IsOutside()
    {
        var persons = new[] { Person("In29", new DateOnly(2000, 1, 18)), Person("In30", new DateOnly(2000, 1, 19)) };

        UpcomingPerson.Within(persons, Today, days: 30).Select(u => u.Person.Name).Should().Equal("In29");
    }

    [Theory]
    [InlineData(12, 20, 6, 1, "Birthday today")]
    [InlineData(6, 1, 12, 21, "Nameday tomorrow")]
    [InlineData(12, 25, 12, 25, "Birthday and nameday in 5 days")]
    public void Description_SaysWhatIsComingAndWhen(int birthdayMonth, int birthdayDay, int nameDayMonth, int nameDayDay,
        string expected)
    {
        var person = Person("Anna", new DateOnly(2000, nameDayMonth, nameDayDay), new DateOnly(1990, birthdayMonth, birthdayDay));

        UpcomingPerson.Create(person, Today).Description.Should().Be(expected);
    }

    [Fact]
    public void PersonWithoutBirthday_UsesTheNameday()
    {
        var upcoming = UpcomingPerson.Create(Person("Anna", new DateOnly(2000, 12, 23)), Today);

        upcoming.DaysUntilBirthday.Should().BeNull();
        upcoming.DaysUntil.Should().Be(3);
        upcoming.Description.Should().Be("Nameday in 3 days");
    }
}
