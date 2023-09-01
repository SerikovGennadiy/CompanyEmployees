using Entities.Excepions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Net.Http.Headers;

namespace CompanyEmployees.Presentation.ActionFilters
{
    public class ValidateMediaTypeAttribute : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            // check Accept header exists
            var acceptHeaderPresent = context
                                        .HttpContext
                                            .Request
                                                .Headers
                                                    .ContainsKey("Accept");
            if(!acceptHeaderPresent)
            {
                context.Result = new BadRequestObjectResult($"Accept header is missing");
                return;
            }

            // check media type is present and valid
            var mediaType = context
                                .HttpContext
                                     .Request
                                         .Headers["Accept"].FirstOrDefault();
            
            if(!MediaTypeHeaderValue.TryParse(mediaType, out MediaTypeHeaderValue? outMediaType))
            {
                context.Result = new BadRequestObjectResult($"Media type not present. " +
                     $"Please add Accept header with the required media type.");
                return;
            }

            // if all its OK add to context our media type and pass to controller!
            context.HttpContext.Items.Add("AcceptHeaderMediaType", outMediaType);
        }

        public void OnActionExecuted(ActionExecutedContext context) { }
    }
}
