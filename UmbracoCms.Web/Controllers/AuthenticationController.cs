using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using UmbracoCms.Web.Infrastructure.Configuration.Options;
using UmbracoCms.Web.Infrastructure.Middlewares;
using UmbracoCms.Web.Utilities;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Web.Website.Controllers;

namespace UmbracoCms.Web.Controllers;

/// <summary>
/// Handles authentication form submission for code-based access.
/// </summary>
public class AuthenticationController : SurfaceController
{
    private readonly AccessTokenOptions _accessTokenOptions;
    
    public AuthenticationController(
        IUmbracoContextAccessor umbracoContextAccessor,
        IUmbracoDatabaseFactory databaseFactory,
        ServiceContext services,
        AppCaches appCaches,
        IProfilingLogger profilingLogger,
        IPublishedUrlProvider publishedUrlProvider,
        IOptions<AccessTokenOptions> accessTokenOptions)
        : base(umbracoContextAccessor, databaseFactory, services, appCaches, profilingLogger, publishedUrlProvider)
    {
        _accessTokenOptions = accessTokenOptions.Value;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SubmitCode(string accessCode, string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(accessCode))
        {
            TempData["Error"] = "Please enter an access code.";
            return CurrentUmbracoPage();
        }

        // Validate the code against the configured shared token
        if (!SharedAccessTokenHelper.TokensMatch(accessCode.Trim(), _accessTokenOptions.SharedToken))
        {
            TempData["Error"] = "Invalid access code. Please try again.";
            return CurrentUmbracoPage();
        }

        // Persist auth token in a cookie (session causes a Serilog enricher stack overflow)
        Response.Cookies.Append(
            SharedAccessTokenExtensions.AuthCookieName,
            accessCode.Trim(),
            SharedAccessTokenExtensions.BuildCookieOptions(HttpContext));

        // Redirect to the return URL if provided and local
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        // Find and redirect to the Sliki page (the main app destination after authentication)
        var slikiPage = Services.ContentService?
            .GetRootContent()
            .FirstOrDefault(x => string.Equals(x.Name, "Sliki", StringComparison.OrdinalIgnoreCase));
        
        if (slikiPage == null)
        {
            // Fall back to any pageHome root content if Sliki is not found
            slikiPage = Services.ContentService?
                .GetRootContent()
                .FirstOrDefault(x => x.ContentType.Alias == "pageHome");
        }

        if (slikiPage != null)
        {
            var publishedContent = UmbracoContext?.Content?.GetById(slikiPage.Id);
            if (publishedContent != null)
            {
                return Redirect(publishedContent.Url() ?? "/");
            }
        }

        return Redirect("/");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        Response.Cookies.Delete(SharedAccessTokenExtensions.AuthCookieName);
        return CurrentUmbracoPage();
    }
}