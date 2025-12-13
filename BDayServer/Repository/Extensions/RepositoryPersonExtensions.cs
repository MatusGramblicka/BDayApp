using Contracts.DataTransferObjects.Person;
using Repository.Extensions.Utility;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;

namespace Repository.Extensions;

public static class RepositoryPersonExtensions
{
    public static IQueryable<PersonDto> Search(this IQueryable<PersonDto> persons, string? searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return persons;

        var lowerCaseTerm = searchTerm.Trim().ToLower();

        return persons.Where(e => e.Name.ToLower().Contains(lowerCaseTerm));
    }

    public static IQueryable<PersonDto> Sort(this IQueryable<PersonDto> persons, string orderByQueryString)
    {
        if (string.IsNullOrWhiteSpace(orderByQueryString))
            return persons
                .OrderBy(BirthMonthOrMax)
                .ThenBy(BirthDayOrMax);

        var orderQuery = OrderQueryBuilder.CreateOrderQuery<PersonDto>(orderByQueryString);

        if (string.IsNullOrWhiteSpace(orderQuery))
            return persons
                .OrderBy(BirthMonthOrMax)
                .ThenBy(BirthDayOrMax);

        if (orderQuery.Contains("DayOfBirth"))
        {
            if (orderQuery.Contains("ascending"))
                return persons
                    .OrderBy(BirthMonthOrMax)
                    .ThenBy(BirthDayOrMax);
            if (orderQuery.Contains("descending"))
                return persons
                    .OrderByDescending(BirthMonthOrMax)
                    .ThenByDescending(BirthDayOrMax);
        }

        if (orderQuery.Contains("DayOfNameDay"))
        {
            if (orderQuery.Contains("ascending"))
                return persons
                    .OrderBy(x => x.DayOfNameDay.Month)
                    .ThenBy(x => x.DayOfNameDay.Day);
            if (orderQuery.Contains("descending"))
                return persons
                    .OrderByDescending(x => x.DayOfNameDay.Month)
                    .ThenByDescending(x => x.DayOfNameDay.Day);
        }

        return persons.OrderBy(orderQuery);
    }

    public static Expression<Func<PersonDto, int>> BirthMonthOrMax =>
        p => p.DayOfBirth.HasValue ? p.DayOfBirth.Value.Month : int.MaxValue;

    public static Expression<Func<PersonDto, int>> BirthDayOrMax =>
        p => p.DayOfBirth.HasValue ? p.DayOfBirth.Value.Day : int.MaxValue;
}