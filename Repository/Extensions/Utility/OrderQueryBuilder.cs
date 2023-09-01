using System.Reflection;
using System.Text;

namespace Repository.Extensions.Utility
{
    public static class OrderQueryBuilder
    {
        public static string CreateOrderQuery<T>(string orderByQueryString)
        {
            //separate query string by individual terms
            var orderParams = orderByQueryString.Split(',');

            // prepare the list of PropertyInfo objects (Employee class properties)
            // for check that class property received through the query string 
            // BindingFlags.Instance - all instance member (члены экземпляра (выделена память) класса, все что не static)
            var propertyInfos = typeof(T).GetProperties(BindingFlags.Public
                                                             | BindingFlags.Instance);

            var orderQueryBuilder = new StringBuilder();
            foreach (var param in orderParams)
            {
                // check params for existence in class by their mentioned up names!
                // check string
                if (string.IsNullOrWhiteSpace(param))
                    continue;

                // extract class property name from term -> age desc -> age
                var propertyFromQueryName = param.Split(" ")[0];

                // find class property by name
                var objectProperty = propertyInfos.FirstOrDefault(pi =>
                            pi.Name.Equals(propertyFromQueryName, StringComparison.InvariantCultureIgnoreCase));

                // if not find continue foreach
                if (objectProperty == null)
                    continue;

                // check our query parametes contains "desc" or "asc"
                var direction = param.EndsWith(" desc") ? "descending" : "ascending";

                // add SQL format another search expression
                orderQueryBuilder.Append($"{objectProperty.Name.ToString()} {direction},");
            }

            // remove excess commas from end our sql expression
            var orderQuery = orderQueryBuilder.ToString().TrimEnd(',', ' ');

            return orderQuery;
        }
    }
}
