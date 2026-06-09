using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.PublishedModels;
using Umbraco.Extensions;

namespace UmbracoCms.Web.Helpers;

public static class ErrorPagesHelper
{
    public const string ErrorPathPrefix = "/#error";

    public static bool IsErrorPath(string path, out int errorCode)
    {
        errorCode = default;
        string errorPathStart = $"{ErrorPathPrefix}/";
        if (!path.StartsWith(errorPathStart, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        string errorCodeStr = path[errorPathStart.Length..].TrimEnd('/');
        return int.TryParse(errorCodeStr, out errorCode);
    }

    public static IPublishedContent? FindErrorPage(this IUmbracoContext umbracoContext, DomainAndUri? domain, int errorCode)
    {
        int? siteId = domain?.ContentId;
        if (siteId is null or <= 0)
        {
            return null;
        }

        IPublishedContent? siteRoot = umbracoContext.Content?.GetByIdAsync(siteId.Value).GetAwaiter().GetResult();
        SiteSettings? siteSettings = siteRoot?.Descendant<SiteSettings>();

        if (siteSettings == null)
        {
            return null;
        }

        return errorCode switch
        {
            StatusCodes.Status404NotFound => siteSettings.UmbracoError404 as IPublishedContent,
            _ => siteSettings.UmbracoError500 as IPublishedContent,
        };
    }
}
