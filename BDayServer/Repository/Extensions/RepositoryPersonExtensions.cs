using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using Contracts.DataTransferObjects.Person;
using Repository.Extensions.Utility;

namespace Repository.Extensions;

public static class RepositoryPersonExtensions
{
    /// <summary>Case-insensitive match on the name or the surname.</summary>
    public static IQueryable<PersonDto> Search(this IQueryable<PersonDto> persons, string? searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return persons;

        var lowerCaseTerm = searchTerm.Trim().ToLower();

        return persons.Where(p => p.Name.ToLower().Contains(lowerCaseTerm) ||
                                  (p.Surname != null && p.Surname.ToLower().Contains(lowerCaseTerm)));
    }

    /// <summary>
    /// Sorts by the first field of the order query. Birthdays and name days sort by month and day (the year doesn't
    /// matter), persons without a birthday last; the default is the birthday. Ties are broken by name and id, so that
    /// paging is stable.
    /// </summary>
    public static IQueryable<PersonDto> Sort(this IQueryable<PersonDto> persons, string? orderByQueryString)
    {
        var orderQuery = OrderQueryBuilder.CreateOrderQuery<PersonDto>(orderByQueryString);
        var firstClause = orderQuery.Split(',')[0].Trim();
        var descending = firstClause.EndsWith(" descending", StringComparison.Ordinal);

        IOrderedQueryable<PersonDto> ordered;

        if (firstClause.Length == 0 ||
            firstClause.StartsWith(nameof(PersonDto.DayOfBirth) + " ", StringComparison.Ordinal))
            ordered = descending
                ? persons.OrderByDescending(BirthMonthOrMax).ThenByDescending(BirthDayOrMax)
                : persons.OrderBy(BirthMonthOrMax).ThenBy(BirthDayOrMax);
        else if (firstClause.StartsWith(nameof(PersonDto.DayOfNameDay) + " ", StringComparison.Ordinal))
            ordered = descending
                ? persons.OrderByDescending(p => p.DayOfNameDay.Month).ThenByDescending(p => p.DayOfNameDay.Day)
                : persons.OrderBy(p => p.DayOfNameDay.Month).ThenBy(p => p.DayOfNameDay.Day);
        else
            ordered = persons.OrderBy(orderQuery);

        return ordered.ThenBy(p => p.Name).ThenBy(p => p.Id);
    }

    public static Expression<Func<PersonDto, int>> BirthMonthOrMax =>
        p => p.DayOfBirth.HasValue ? p.DayOfBirth.Value.Month : int.MaxValue;

    public static Expression<Func<PersonDto, int>> BirthDayOrMax =>
        p => p.DayOfBirth.HasValue ? p.DayOfBirth.Value.Day : int.MaxValue;
}
