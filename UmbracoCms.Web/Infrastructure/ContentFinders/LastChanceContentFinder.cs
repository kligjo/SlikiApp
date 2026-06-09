using UmbracoCms.Web.Helpers;
using UmbracoCms.Web.Infrastructure.Configuration.Options;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Web;

namespace UmbracoCms.Web.Infrastructure.ContentFinders;

public class LastChanceContentFinder : IContentLastChanceFinder
{
    private readonly IUmbracoContextAccessor _umbracoContextAccessor;
    private readonly IOptionsMonitor<ApplicationOptions> _applicationOptions;

    public LastChanceContentFinder(
        IUmbracoContextAccessor umbracoContextAccessor,
        IOptionsMonitor<ApplicationOptions> applicationOptions)
    {
        _umbracoContextAccessor = umbracoContextAccessor;
        _applicationOptions = applicationOptions;
    }

    public Task<bool> TryFindContent(IPublishedRequestBuilder request)
    {
        if (!_umbracoContextAccessor.TryGetUmbracoContext(out var umbracoContext))
        {
            return Task.FromResult(false);
        }

        string path = request.AbsolutePathDecoded;
        int errorCode;

        if (ErrorPagesHelper.IsErrorPath(path, out errorCode))
        {
            // Restore original path if this is a re-execution
            if (request.Uri.GetLeftPart(UriPartial.Authority) is { } authority)
            {
                string originalPath = path.Replace(ErrorPagesHelper.ErrorPathPrefix, "");
                if (!string.IsNullOrEmpty(originalPath) && originalPath != "/")
                {
                    // Path was changed by error handler
                }
            }
        }
        else
        {
            errorCode = StatusCodes.Status404NotFound;
        }

        var errorPage = umbracoContext.FindErrorPage(request.Domain, errorCode);
        if (errorPage == null)
        {
            return Task.FromResult(false);
        }

        request.SetPublishedContent(errorPage);

        // Disable caching for error responses
        request.SetNoCacheHeader(true);

        return Task.FromResult(true);
    }
}
