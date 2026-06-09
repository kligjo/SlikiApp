using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace UmbracoCms.Web.Api;

/// <summary>
/// Fallback route handler for Api Endpoints that returns an appropriate 404 response.
/// </summary>
public static class ApiFallbackRouteHandler
{
    /// <summary>
    /// Registers the <see cref="ApiFallbackRouteHandler"/> to handle not found Api Routes.
    /// </summary>
    public static IEndpointRouteBuilder UseApiFallbackRoute(this IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder.Map($"/{ApiControllerBase.ApiRoutePrefix}/{{**slug}}", HandleNotFoundApiRoute);
        return endpointRouteBuilder;
    }

    private static Task HandleNotFoundApiRoute(HttpContext context, [FromServices] ProblemDetailsFactory problemDetailsFactory)
    {
        ProblemDetails problemDetails = problemDetailsFactory.CreateProblemDetails(context, statusCode: StatusCodes.Status404NotFound, title: "Endpoint Not Found");
        return new ObjectResult(problemDetails).ExecuteResultAsync(new ActionContext { HttpContext = context });
    }
}
