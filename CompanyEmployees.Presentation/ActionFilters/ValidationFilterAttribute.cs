using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CompanyEmployees.Presentaion.ActionFilters
{
    public class ValidationFilterAttribute : IActionFilter
    {
        public ValidationFilterAttribute()
        { }
        public void OnActionExecuting(ActionExecutingContext context)
        {
            // RouteData.Values - key-value dict, which inits during routing to current routing path
            var action = context.RouteData.Values["action"];
            var controller = context.RouteData.Values["controller"];
            
            // ActionArguments allows access to action params. DTO objects sended by POST or PUT
            var param = context.ActionArguments
                .SingleOrDefault(x => x.Value.ToString().Contains("DTO")).Value;

            // handling checking param for null. init context.Result
            if (param is null)
            {
                context.Result =
                    new BadRequestObjectResult($"Object is null. Controller: {controller}, action: {action}");
                return;
            }
 
            // handling for chacking param for valid
            if (!context.ModelState.IsValid)
                context.Result = new UnprocessableEntityObjectResult(context.ModelState);
        }
        public void OnActionExecuted(ActionExecutedContext context)
        { }

    }
}
