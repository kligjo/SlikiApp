using System.Net.Mime;
using UmbracoCms.Web.Infrastructure.Configuration.Options;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace UmbracoCms.Web.Helpers;

public static class RobotsHelper
{
    public static IEndpointRouteBuilder UseRobots(this IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder.MapGet("/robots.txt", GenerateRobots);
        return endpointRouteBuilder;
    }

    public static Task GenerateRobots(HttpContext context, [FromServices] IOptionsMonitor<ApplicationOptions> applicationOptions)
    {
        Uri currentUri = new(context.Request.GetDisplayUrl());
        string output = applicationOptions.CurrentValue.IsCrawlableUrl(currentUri) switch
        {
            true => "User-agent: *\n" +
                    "Allow: /\n" +
                    $"Sitemap: {context.Request.Scheme}://{context.Request.Host}/sitemap.xml",
            _ => "User-agent: *\n" +
                 "Disallow: /",
        };

        context.Response.ContentType = MediaTypeNames.Text.Plain;
        return context.Response.WriteAsync(output);
    }
}
