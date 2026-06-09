using Microsoft.Extensions.Options;
using UmbracoCms.Web.Infrastructure.Configuration.Options;
using UmbracoCms.Web.Utilities;

namespace UmbracoCms.Web.Infrastructure.Middlewares;

public static class SharedAccessTokenExtensions
{
    private static readonly PathString[] ExcludedPrefixes =
    [
        new("/umbraco"),
        new("/App_Plugins"),
        new("/install"),
        new("/mini-profiler-resources")
    ];

    public static IApplicationBuilder UseSharedAccessToken(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            if (IsExcludedPath(context.Request.Path))
            {
                await next();
                return;
            }

            var options = context.RequestServices.GetRequiredService<IOptions<AccessTokenOptions>>().Value;
            if (!SharedAccessTokenHelper.TryResolveRequestToken(context, options, out var requestToken)
                || !SharedAccessTokenHelper.TokensMatch(requestToken, options.SharedToken))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Unauthorized");
                return;
            }

            await next();
        });
    }

    private static bool IsExcludedPath(PathString requestPath) =>
        ExcludedPrefixes.Any(prefix => requestPath.StartsWithSegments(prefix, StringComparison.OrdinalIgnoreCase));
}
