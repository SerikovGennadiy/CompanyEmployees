using Microsoft.AspNetCore.Mvc.Filters;

namespace ActionFilters.Filters
{
    // async version have only 1 method, instead of sync version (ActionFilterExample)
    internal class AsyncActionFilterAttribute : IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            // execute any code before the action executes
            var result = await next();
            // execute any code after action executes
        }
    }
}
