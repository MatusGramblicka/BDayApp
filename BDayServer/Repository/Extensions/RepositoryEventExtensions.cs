using Entities.Models;
using Repository.Extensions.Utility;
using System.Linq;
using System.Linq.Dynamic.Core;

namespace Repository.Extensions
{
    public static class RepositoryEventExtensions
    {
        public static IQueryable<Event> Search(this IQueryable<Event> events, string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return events;

            var lowerCaseTerm = searchTerm.Trim().ToLower();

            return events.Where(e => e.Name.ToLower().Contains(lowerCaseTerm));
        }

        public static IQueryable<Event> Sort(this IQueryable<Event> events, string orderByQueryString)
        {
            if (string.IsNullOrWhiteSpace(orderByQueryString))
                return events.OrderBy(x => x.Date.Month).ThenBy(x => x.Date.Day);

            var orderQuery = OrderQueryBuilder.CreateOrderQuery<Event>(orderByQueryString);

            if (string.IsNullOrWhiteSpace(orderQuery))
                return events.OrderBy(x => x.Date.Month).ThenBy(x => x.Date.Day);

            if (orderQuery.Contains("DayOfBirth"))
            {
                if (orderQuery.Contains("ascending"))
                    return events.OrderBy(x => x.Date.Month).ThenBy(x => x.Date.Day);
                if (orderQuery.Contains("descending"))
                    return events.OrderByDescending(x => x.Date.Month).ThenByDescending(x => x.Date.Day);
            }

            if (orderQuery.Contains("DayOfNameDay"))
            {
                if (orderQuery.Contains("ascending"))
                    return events.OrderBy(x => x.Date.Month).ThenBy(x => x.Date.Day);
                if (orderQuery.Contains("descending"))
                    return events.OrderByDescending(x => x.Date.Month)
                        .ThenByDescending(x => x.Date.Day);
            }

            return events.OrderBy(orderQuery);
        }
    }
}
