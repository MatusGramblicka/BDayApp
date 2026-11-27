using Contracts.DataTransferObjects.Person;

namespace BDayClient.Features;

/// <summary>A person with the days left until their next birthday and nameday.</summary>
public sealed record UpcomingPerson(PersonDto Person, int? DaysUntilBirthday, int DaysUntilNameDay)
{
    /// <summary>Days until the sooner of the two (0 = today).</summary>
    public int DaysUntil => Math.Min(DaysUntilBirthday ?? int.MaxValue, DaysUntilNameDay);

    /// <summary>E.g. "Birthday today", "Nameday tomorrow", "Birthday and nameday in 3 days".</summary>
    public string Description
    {
        get
        {
            var occasion = (DaysUntilBirthday == DaysUntil, DaysUntilNameDay == DaysUntil) switch
            {
                (true, true) => "Birthday and nameday",
                (true, false) => "Birthday",
                _ => "Nameday"
            };

            var when = DaysUntil switch
            {
                0 => "today",
                1 => "tomorrow",
                _ => $"in {DaysUntil} days"
            };

            return $"{occasion} {when}";
        }
    }

    public static UpcomingPerson Create(PersonDto person, DateOnly today) => new(
        person,
        person.DayOfBirth is { } birthday ? DayMonth.DaysUntilNextOccurrence(birthday, today) : null,
        DayMonth.DaysUntilNextOccurrence(person.DayOfNameDay, today));

    /// <summary>
    /// The persons whose birthday or nameday is within the next <paramref name="days"/> days (today included),
    /// soonest first.
    /// </summary>
    public static IReadOnlyList<UpcomingPerson> Within(IEnumerable<PersonDto> persons, DateOnly today, int days) =>
        persons
            .Select(person => Create(person, today))
            .Where(upcoming => upcoming.DaysUntil < days)
            .OrderBy(upcoming => upcoming.DaysUntil)
            .ThenBy(upcoming => upcoming.Person.Name, StringComparer.CurrentCulture)
            .ThenBy(upcoming => upcoming.Person.Surname, StringComparer.CurrentCulture)
            .ToList();
}
