using Entities.Models;
using Repository.Extensions.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Linq.Dynamic.Core;

namespace Repository.Extensions
{
    public static class RepositoryPersonExtensions
    {
        //public static IQueryable<Person> FilterEmployees(this IQueryable<Person> employees, uint minAge, uint maxAge) =>
        //    employees.Where(e => (e.Age >= minAge && e.Age <= maxAge));

        public static IQueryable<Person> Search(this IQueryable<Person> persons, string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return persons;

            var lowerCaseTerm = searchTerm.Trim().ToLower();

            return persons.Where(e => e.Name.ToLower().Contains(lowerCaseTerm));
        }

        public static IQueryable<Person> Sort(this IQueryable<Person> persons, string orderByQueryString)
        {
            if (string.IsNullOrWhiteSpace(orderByQueryString))
                return persons.OrderBy(e => e.Name);

            var orderQuery = OrderQueryBuilder.CreateOrderQuery<Person>(orderByQueryString);

            if (string.IsNullOrWhiteSpace(orderQuery))
                return persons.OrderBy(e => e.Name);

            return persons.OrderBy(orderQuery);
        }
    }
}
