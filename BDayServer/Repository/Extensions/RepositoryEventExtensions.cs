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
    /// Sorts by the order query; without one, by the date's month and day. Ties are broken by id, so that paging is stable.
    /// </summary>
    public static IQueryable<Event> Sort(this IQueryable<Event> events, string? orderByQueryString)
    {
        var orderQuery = OrderQueryBuilder.CreateOrderQuery<Event>(orderByQueryString);

        var ordered = string.IsNullOrWhiteSpace(orderQuery)
            ? events.OrderBy(e => e.Date.Month).ThenBy(e => e.Date.Day)
            : events.OrderBy(orderQuery);

        return ordered.ThenBy(e => e.Id);
    }
}
