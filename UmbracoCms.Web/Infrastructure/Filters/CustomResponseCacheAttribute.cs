using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Net.Http.Headers;

namespace UmbracoCms.Web.Infrastructure.Filters;

/// <summary>
/// Custom response cache attribute that integrates with the CacheManager.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class CustomResponseCacheAttribute : Attribute, IActionFilter, IOrderedFilter
{
    public int Duration { get; set; }

    /// <summary>
    /// Server-side cache duration in seconds. Use -1 to indicate no server duration.
    /// </summary>
    public int ServerDuration { get; set; } = -1;

    public ResponseCacheLocation Location { get; set; } = ResponseCacheLocation.Any;

    public bool NoStore { get; set; }

    public string? VaryByHeader { get; set; }

    public string[]? VaryByQueryKeys { get; set; }

    public int Order { get; set; }

    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (!NoStore && Duration <= 0)
        {
            throw new InvalidOperationException("Duration must be set when NoStore is false.");
        }

        if (ServerDuration >= 0 && Location != ResponseCacheLocation.Any)
        {
            throw new InvalidOperationException("ServerDuration can only be used with Location.Any.");
        }

        var cacheManager = context.HttpContext.RequestServices.GetRequiredService<ICacheManager>();
        if (!cacheManager.ShouldRequestBeCached(context.HttpContext))
        {
            return;
        }

        var headers = context.HttpContext.Response.Headers;

        if (NoStore)
        {
            headers[HeaderNames.CacheControl] = "no-store";
            headers[HeaderNames.Pragma] = "no-cache";
            return;
        }

        string cacheControl = Location switch
        {
            ResponseCacheLocation.Any => $"public, max-age={Duration}" + (ServerDuration >= 0 ? $", s-maxage={ServerDuration}" : ""),
            ResponseCacheLocation.Client => $"private, max-age={Duration}",
            ResponseCacheLocation.None => "no-cache",
            _ => $"public, max-age={Duration}",
        };

        headers[HeaderNames.CacheControl] = cacheControl;
        headers[HeaderNames.LastModified] = cacheManager.LastCacheFlush.ToString("R");

        if (!string.IsNullOrEmpty(VaryByHeader))
        {
            headers[HeaderNames.Vary] = VaryByHeader;
        }

        if (VaryByQueryKeys?.Length > 0)
        {
            var responseCachingFeature = context.HttpContext.Features.Get<Microsoft.AspNetCore.ResponseCaching.IResponseCachingFeature>();
            if (responseCachingFeature != null)
            {
                responseCachingFeature.VaryByQueryKeys = VaryByQueryKeys;
            }
        }
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}
