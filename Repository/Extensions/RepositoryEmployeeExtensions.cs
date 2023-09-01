using Entities.Models;
using Repository.Extensions.Utility;
using System.Linq.Dynamic.Core;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace Repository.Extensions
{
    public static class RepositoryEmployeeExtensions
    {
        public static IQueryable<Employee> FilterEmployees(this IQueryable<Employee> employees,
            uint minAge, uint maxAge) =>
            employees.Where(e => (e.Age >= minAge) && (e.Age <= maxAge));

        public static IQueryable<Employee> Search(this IQueryable<Employee> employees, string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return employees;

            var lowerCaseTerm = searchTerm.Trim().ToLower();

            return employees.Where(e => e.Name.ToLower().Contains(lowerCaseTerm));
        }
        
        // https://localhost:5001/api/controllers/companyId/employees?orderBy=name,age desc
        // orderByQueryString will be ( name, age desc ). We work in Sort with it

        // THIS IS LITTLE TRICK TO FORM QUERY WHEN YOU DON'T KNOW IN ADVANCE HOW YOU SHOULD SORT
        public static IQueryable<Employee> Sort(this IQueryable<Employee> employees, string orderByQueryString)
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
