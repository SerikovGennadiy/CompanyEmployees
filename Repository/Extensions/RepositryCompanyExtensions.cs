using Entities.Models;
using Repository.Extensions.Utility;
using System.Linq.Dynamic.Core;

namespace Repository.Extensions
{
    public static class RepositryCompanyExtensions
    {
        public static IQueryable<Company> Sort(this IQueryable<Company> employees, string orderByQueryString)
        {
            // simple check for null or white space
            if (string.IsNullOrWhiteSpace(orderByQueryString))
                return employees.OrderBy(e => e.Name);

            // in beginnig this logic of this UTILITY method was here, but bacause it with high
            // propability can reuse for different Types we extract this logic in UTILITY generic method!!!
            var orderQuery = OrderQueryBuilder.CreateOrderQuery<Employee>(orderByQueryString);

            // if orderQuery is null apply standart Linq
            if (string.IsNullOrWhiteSpace(orderQuery))
                return employees.OrderBy(e => e.Name);

            // apply orderQuery if exists
            return employees.OrderBy(orderQuery);
        }
    }
}
