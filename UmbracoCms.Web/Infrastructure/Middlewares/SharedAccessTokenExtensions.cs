using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;
using UmbracoCms.Web.Infrastructure.Configuration.Options;
using UmbracoCms.Web.Utilities;

namespace UmbracoCms.Web.Infrastructure.Middlewares;

/// <summary>
/// Extension methods for registering the shared access token middleware.
/// </summary>
public static class SharedAccessTokenExtensions
{
    // Cookie-based auth avoids the DistributedSession.Load() ↔ Serilog HttpSessionIdEnricher
    // re-entrancy stack overflow that occurs with session-based storage at this pipeline position.
    internal const string AuthCookieName = "SlikiAuth";
    private const string DefaultAuthPagePath = "/auth";
    
    private static readonly HashSet<string> ExcludedPaths =
    [
        "/umbraco",
        "/install",
        "/api",
        "/auth"  // Exclude the auth page itself to prevent infinite redirect loop
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

            // Check auth cookie
            string? cookieToken = context.Request.Cookies[AuthCookieName];
            if (!string.IsNullOrWhiteSpace(cookieToken)
                && SharedAccessTokenHelper.TokensMatch(cookieToken, options.SharedToken))
            {
                await next();
                return;
            }
            
            // Fall back to query string token (for backward compatibility)
            if (SharedAccessTokenHelper.TryResolveRequestToken(context, options, out var requestToken)
                && SharedAccessTokenHelper.TokensMatch(requestToken, options.SharedToken))
            {
                // Persist in cookie for subsequent requests
                context.Response.Cookies.Append(AuthCookieName, requestToken, BuildCookieOptions(context));
                await next();
                return;
            }

            // Redirect to authentication page using fixed path
            var returnUrl = $"{context.Request.Path}{context.Request.QueryString}";
            var authUrl = $"{DefaultAuthPagePath}?returnUrl={Uri.EscapeDataString(returnUrl)}";
            context.Response.Redirect(authUrl);
        });
    }

    internal static CookieOptions BuildCookieOptions(HttpContext context) => new()
    {
        HttpOnly = true,
        SameSite = SameSiteMode.Lax,
        Secure = context.Request.IsHttps,
        MaxAge = TimeSpan.FromHours(8)
    };

    private static bool IsExcludedPath(PathString path)
    {
        return ExcludedPaths.Any(excluded => path.StartsWithSegments(excluded, StringComparison.OrdinalIgnoreCase));
    }
}
