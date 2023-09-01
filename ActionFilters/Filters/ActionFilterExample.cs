using Microsoft.AspNetCore.Mvc.Filters;

namespace ActionFilters.Filters
{
    internal class ActionFilterExample : IActionFilter
    {
        public void OnActionExecuted(ActionExecutedContext context)
        {
            // out code before action executes
            throw new NotImplementedException();
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            // our code after action executes
            throw new NotImplementedException();
        }
    }
}
