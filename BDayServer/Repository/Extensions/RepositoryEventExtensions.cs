using System.Linq.Dynamic.Core;
using Entities.Models;
using Repository.Extensions.Utility;

namespace Repository.Extensions;

public static class RepositoryEventExtensions
{
    public static IQueryable<Event> Search(this IQueryable<Event> events, string? searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return events;

        var lowerCaseTerm = searchTerm.Trim().ToLower();

        return events.Where(e => e.Name.ToLower().Contains(lowerCaseTerm));
    }

    /// <summary>
    /// Sorts by the first field of the order query. The date sorts by month and day - the year isn't shown, like for a
    /// person's birthday - and is the default. Ties are broken by name and id, so that paging is stable.
    /// </summary>
    public static IQueryable<Event> Sort(this IQueryable<Event> events, string? orderByQueryString)
    {
        var orderQuery = OrderQueryBuilder.CreateOrderQuery<Event>(orderByQueryString);
        var firstClause = orderQuery.Split(',')[0].Trim();

        IOrderedQueryable<Event> ordered;

        if (firstClause.Length == 0 || firstClause.StartsWith(nameof(Event.Date) + " ", StringComparison.Ordinal))
            ordered = firstClause.EndsWith(" descending", StringComparison.Ordinal)
                ? events.OrderByDescending(e => e.Date.Month).ThenByDescending(e => e.Date.Day)
                : events.OrderBy(e => e.Date.Month).ThenBy(e => e.Date.Day);
        else
            ordered = events.OrderBy(orderQuery);

        return ordered.ThenBy(e => e.Name).ThenBy(e => e.Id);
    }
}
