using UmbracoCms.Web.Infrastructure.DependencyInjection;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.PublishedModels;

namespace UmbracoCms.Web.Services;

[Scoped]
public class NodeProvider
{
    private readonly IUmbracoContextAccessor _umbracoContextAccessor;

    private PageHome? _homePage;
    private SiteSettings? _siteSettings;

    public NodeProvider(IUmbracoContextAccessor umbracoContextAccessor)
    {
        _umbracoContextAccessor = umbracoContextAccessor;
    }

    public PageHome? HomePage => _homePage ??= GetHomePage();

    public SiteSettings? SiteSettings => _siteSettings ??= GetSiteSettings();

    public IPublishedContent? GetCurrentNode()
    {
        if (!_umbracoContextAccessor.TryGetUmbracoContext(out var context))
        {
            return null;
        }

        return context.PublishedRequest?.PublishedContent;
    }

    internal void Reset()
    {
        _homePage = null;
        _siteSettings = null;
    }

    private PageHome? GetHomePage()
    {
        var currentNode = GetCurrentNode();
        return currentNode?.AncestorOrSelf<PageHome>();
    }

    private SiteSettings? GetSiteSettings()
    {
        return HomePage?.Descendant<SiteSettings>();
    }
}
