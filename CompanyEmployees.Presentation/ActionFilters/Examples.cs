using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompanyEmployees.Presentation.ActionFilters
{
    internal class FilterExample : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            // this code before action executes
            throw new NotImplementedException();
        }
        public void OnActionExecuted(ActionExecutedContext context)
        {
            // this code after action executes
            throw new NotImplementedException();
        }
    }

    internal class FilterExampleAsync : IAsyncActionFilter
    {
        // in async version is only one method
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            // code before action method
            var result = await next();
            // code after action method
        }
    }
}
