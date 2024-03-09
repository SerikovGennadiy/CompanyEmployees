using Contracts;
using Entities.ErrorModel;
using Entities.Excepions;
using Entities.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using System.Net;
using System.Text.Json;

namespace CompanyEmployees.Extensions
{
    public static class ExceptionMiddlewareExtensions
    {
        public static void ConfigureExceptionHandler(this WebApplication app,
            ILoggerManager logger)
        {
            app.UseExceptionHandler((IApplicationBuilder appError) =>
            {
                appError.Run(async context =>
                {
                    context.Response.ContentType = "application/json";

                    var contextFeature = context.Features.Get<IExceptionHandlerFeature>();
                    if (contextFeature != null)
                    {
                        context.Response.StatusCode = contextFeature.Error switch
                        {
                            NotFoundException => StatusCodes.Status404NotFound,
                            BadRequestException => StatusCodes.Status400BadRequest,
                            // MedidatR Behavior (validation error) (см. Program.cs CQRS)
                            // check for exception validation
                            ValidationAppException => StatusCodes.Status422UnprocessableEntity,
                            _ => StatusCodes.Status500InternalServerError
                        };

                        logger.LogError($"Something went wrong: {contextFeature.Error}");

                        // test exception type (this validatnion)
                        if (contextFeature.Error is ValidationAppException exception)
                        {
                            // return and serialize all validation errors
                            await context.Response.WriteAsync(JsonSerializer.Serialize(new
                            {
                                exception.Errors
                            }));
                        }
                        else
                        {
                            await context.Response.WriteAsync(new ErrorDetails()
                            {
                                StatusCode = context.Response.StatusCode,
                                Message = contextFeature.Error.Message
                            }.ToString());
                        }
                    }
                });
            });
        }
    }
}
