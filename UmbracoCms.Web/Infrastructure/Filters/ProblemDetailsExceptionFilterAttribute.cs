using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace UmbracoCms.Web.Infrastructure.Filters;

public class ProblemDetailsExceptionFilterAttribute : ExceptionFilterAttribute
{
    public override void OnException(ExceptionContext context)
    {
        var logger = context.HttpContext.RequestServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger(context.ActionDescriptor.DisplayName ?? "UnknownAction");

        logger.LogError(context.Exception, "Unhandled exception occurred");

        var factory = context.HttpContext.RequestServices.GetRequiredService<ProblemDetailsFactory>();
        var problemDetails = factory.CreateProblemDetails(
            context.HttpContext,
            statusCode: StatusCodes.Status500InternalServerError,
            title: "An unexpected error occurred");

        context.Result = new ObjectResult(problemDetails)
        {
            StatusCode = StatusCodes.Status500InternalServerError,
        };

        context.ExceptionHandled = true;
    }
}
